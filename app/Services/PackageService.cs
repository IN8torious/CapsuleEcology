using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CapsuleEcology.Services
{
    /// <summary>
    /// Environment Capsules (R10): curated app packs installed through
    /// winget (primary — ships with Windows, resolves to upstream vendor
    /// installers) or Chocolatey (optional second source). Capsule is the
    /// orchestrator, never the warehouse: every install runs the Capsule
    /// Order — restore point first, manifest disclosed up front, user
    /// initiates, one receipt per app (id, resolved version, source),
    /// uninstall mirror for the whole pack. The SI kernel's spine —
    /// proposal, capability, receipt — is the manager; winget/choco are
    /// only the trucks.
    /// </summary>
    public sealed class PackageService
    {
        private readonly ReceiptLog _log;
        private bool? _hasWinget, _hasChoco;

        public PackageService(ReceiptLog log) { _log = log; }

        public async Task<(bool Winget, bool Choco)> DetectSourcesAsync()
        {
            _hasWinget ??= (await RunAsync("winget", new[] { "--version" }, 20)).Exit == 0;
            _hasChoco ??= (await RunAsync("choco", new[] { "--version" }, 20)).Exit == 0;
            return (_hasWinget.Value, _hasChoco.Value);
        }

        private (string Source, string Id)? ChooseSource(PackApp app, bool hasWinget, bool hasChoco)
        {
            if (hasWinget && app.Winget.Length > 0) return ("winget", app.Winget);
            if (hasChoco && app.Choco.Length > 0) return ("chocolatey", app.Choco);
            return null;
        }

        public string PlanText(Pack pack, IReadOnlyList<PackApp> apps, bool hasWinget, bool hasChoco)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{pack.Name} — {apps.Count} apps selected of {pack.Apps.Count}.");
            sb.AppendLine("Disclosed before anything downloads: what, and from where.");
            sb.AppendLine();
            foreach (var a in apps)
            {
                var src = ChooseSource(a, hasWinget, hasChoco);
                sb.AppendLine($"— {a.Name} ({(a.Optional ? "optional" : "core")})");
                sb.AppendLine(src is null
                    ? "  NO SOURCE AVAILABLE on this machine (needs winget or Chocolatey)"
                    : $"  from {src.Value.Source}: {src.Value.Id}");
                if (a.Note.Length > 0) sb.AppendLine($"  {a.Note}");
            }
            foreach (var d in pack.Docs) { sb.AppendLine(); sb.AppendLine(d); }
            sb.AppendLine();
            sb.AppendLine("A restore point is created before the first install. One receipt per app,");
            sb.AppendLine("recording the version the source actually installed. Uninstall mirrors the pack.");
            return sb.ToString();
        }

        public async Task<List<StepResult>> InstallAsync(Pack pack, IReadOnlyList<PackApp> apps, Action<string>? progress = null)
        {
            var results = new List<StepResult>();
            var (hasWinget, hasChoco) = await DetectSourcesAsync();
            var engine = new TweakEngine(_log);
            if (!engine.EnsureRestorePoint($"Capsule Ecology — before installing {pack.Name}", out var note))
            {
                results.Add(new StepResult { Description = "Restore point", Ok = false, Note = note });
                return results;
            }
            int ok = 0;
            foreach (var app in apps)
            {
                progress?.Invoke($"Installing {app.Name}…");
                var r = await InstallOneAsync(app, hasWinget, hasChoco);
                if (r.Ok) ok++;
                results.Add(r);
                _log.Seal("package-manager", r.Ok ? "install" : "install-failed",
                    $"install {app.Name} [{pack.Id}] {r.Note}");
            }
            _log.Seal("package-manager", "install-pack", $"pack {pack.Id}: {ok}/{apps.Count} apps installed");
            return results;
        }

        public async Task<List<StepResult>> UninstallAsync(Pack pack, IReadOnlyList<PackApp> apps, Action<string>? progress = null)
        {
            var results = new List<StepResult>();
            var (hasWinget, hasChoco) = await DetectSourcesAsync();
            var engine = new TweakEngine(_log);
            if (!engine.EnsureRestorePoint($"Capsule Ecology — before uninstalling {pack.Name}", out var note))
            {
                results.Add(new StepResult { Description = "Restore point", Ok = false, Note = note });
                return results;
            }
            int ok = 0;
            foreach (var app in apps)
            {
                progress?.Invoke($"Uninstalling {app.Name}…");
                var r = await UninstallOneAsync(app, hasWinget, hasChoco);
                if (r.Ok) ok++;
                results.Add(r);
                _log.Seal("package-manager", r.Ok ? "uninstall" : "uninstall-failed",
                    $"uninstall {app.Name} [{pack.Id}] {r.Note}");
            }
            _log.Seal("package-manager", "uninstall-pack", $"pack {pack.Id}: {ok}/{apps.Count} apps uninstalled");
            return results;
        }

        private async Task<StepResult> InstallOneAsync(PackApp app, bool hasWinget, bool hasChoco)
        {
            var src = ChooseSource(app, hasWinget, hasChoco);
            if (src is null)
                return new StepResult { Description = app.Name, Ok = false, Note = "no source available (winget/Chocolatey missing)" };
            var (source, id) = src.Value;
            var args = source == "winget"
                ? new[] { "install", "--id", id, "-e", "--accept-source-agreements", "--accept-package-agreements", "--silent", "--disable-interactivity" }
                : new[] { "install", id, "-y", "--no-progress" };
            var run = await RunAsync(source == "winget" ? "winget" : "choco", args, 900);
            var version = run.Exit == 0 ? await ResolveVersionAsync(app, source, id) : "unknown";
            return new StepResult
            {
                Description = app.Name,
                Ok = run.Exit == 0,
                Note = $"via {source} id={id} version={version} exit={run.Exit}" + (run.Exit == 0 ? "" : $" :: {run.Tail}"),
            };
        }

        private async Task<StepResult> UninstallOneAsync(PackApp app, bool hasWinget, bool hasChoco)
        {
            var src = ChooseSource(app, hasWinget, hasChoco);
            if (src is null)
                return new StepResult { Description = app.Name, Ok = false, Note = "no source available (winget/Chocolatey missing)" };
            var (source, id) = src.Value;
            var args = source == "winget"
                ? new[] { "uninstall", "--id", id, "-e", "--silent", "--disable-interactivity" }
                : new[] { "uninstall", id, "-y" };
            var run = await RunAsync(source == "winget" ? "winget" : "choco", args, 600);
            return new StepResult
            {
                Description = app.Name,
                Ok = run.Exit == 0,
                Note = $"via {source} id={id} exit={run.Exit}" + (run.Exit == 0 ? "" : $" :: {run.Tail}"),
            };
        }

        private async Task<string> ResolveVersionAsync(PackApp app, string source, string id)
        {
            try
            {
                if (source == "winget")
                {
                    var run = await RunAsync("winget", new[] { "list", "--id", id, "-e", "--accept-source-agreements" }, 60);
                    foreach (var line in run.Full.Split('\n'))
                    {
                        if (!line.Contains(id)) continue;
                        var m = Regex.Match(line, @"\d+(\.\d+){1,3}");
                        if (m.Success) return m.Value;
                    }
                }
                else
                {
                    var run = await RunAsync("choco", new[] { "list", "--local-only", "--exact", id, "-r" }, 60);
                    var parts = run.Full.Trim().Split('|');
                    if (parts.Length == 2 && parts[1].Trim().Length > 0) return parts[1].Trim();
                }
            }
            catch { /* version is best-effort; the receipt says unknown rather than inventing one */ }
            return "unknown";
        }

        private static async Task<(int Exit, string Tail, string Full)> RunAsync(string file, IEnumerable<string> args, int timeoutSec)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = file,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                };
                foreach (var a in args) psi.ArgumentList.Add(a);
                using var p = new Process { StartInfo = psi };
                p.Start();
                var outTask = p.StandardOutput.ReadToEndAsync();
                var errTask = p.StandardError.ReadToEndAsync();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSec));
                try { await p.WaitForExitAsync(cts.Token); }
                catch (OperationCanceledException)
                {
                    try { p.Kill(entireProcessTree: true); } catch { }
                    return (-1, $"timeout after {timeoutSec}s", "");
                }
                var full = (await outTask) + await errTask;
                var tail = full.Length > 300 ? full[^300..].Replace("\n", " ").Trim() : full.Replace("\n", " ").Trim();
                return (p.ExitCode, tail, full);
            }
            catch (Exception ex)
            {
                return (-2, ex.Message, "");
            }
        }
    }
}
