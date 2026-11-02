using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CapsuleEcology.Services
{
    /// <summary>
    /// The Repair face (R3): the 0.2.2 repair toolkit, native.
    /// Two halves — (1) the toolkit itself: the shipped scripts install
    /// to %ProgramData%\CapsuleOS\Repair with desktop/Start shortcuts
    /// and uninstall cleanly (the same Install/Uninstall pair the
    /// playbook carries). (2) The repair run: Microsoft's own order —
    /// DISM CheckHealth, ScanHealth, RestoreHealth, then SFC, then a
    /// confirming SFC — streamed live, logged to a dated file, every
    /// step sealed. DISM/SFC need administrator; when the app is not
    /// elevated the run refuses plainly instead of half-running (the
    /// installed toolkit's .cmd self-elevates for that case).
    /// </summary>
    public sealed class RepairService
    {
        private readonly ReceiptLog _log;
        public RepairService(ReceiptLog log) { _log = log; }

        public static string ToolkitDir => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "CapsuleOS", "Repair");

        public bool ToolkitInstalled => File.Exists(Path.Combine(ToolkitDir, "Repair-Windows.ps1"));

        public static bool IsElevated()
        {
            try
            {
                using var id = WindowsIdentity.GetCurrent();
                return new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch { return false; }
        }

        private static string ScriptPath(string name) => Path.Combine(AppContext.BaseDirectory, "Assets", "scripts", name);

        public async Task<(bool Ok, string Note)> InstallToolkitAsync()
        {
            var r = await RunScriptAsync(ScriptPath("Install-CapsuleRepair.ps1"));
            if (r.Exit == 0) _log.Seal("repair", "toolkit-install", $"repair toolkit installed to {ToolkitDir}");
            return (r.Exit == 0, r.Exit == 0 ? $"Toolkit installed to {ToolkitDir} — 'Repair Windows' shortcuts added." : $"Install failed (exit {r.Exit}): {r.Tail}");
        }

        public async Task<(bool Ok, string Note)> UninstallToolkitAsync()
        {
            var r = await RunScriptAsync(ScriptPath("Uninstall-CapsuleRepair.ps1"));
            if (r.Exit == 0) _log.Seal("repair", "toolkit-uninstall", "repair toolkit removed");
            return (r.Exit == 0, r.Exit == 0 ? "Toolkit removed — files and shortcuts gone." : $"Uninstall failed (exit {r.Exit}): {r.Tail}");
        }

        public async Task<(bool Ok, string Note)> RunFullRepairAsync(Action<string>? progress = null)
        {
            if (!IsElevated())
                return (false, "The repair run needs administrator. Either run Capsule Ecology as administrator, or use the installed toolkit's 'Repair Windows' shortcut — it self-elevates. Nothing was run.");
            var logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CapsuleEcology", "repair-logs");
            Directory.CreateDirectory(logDir);
            var logPath = Path.Combine(logDir, $"repair-{DateTime.Now:yyyyMMdd-HHmmss}.log");
            var log = new StringBuilder($"Capsule Ecology repair run — {DateTime.Now}\r\n");
            var steps = new (string Label, string File, string Args)[]
            {
                ("DISM CheckHealth", "dism.exe", "/Online /Cleanup-Image /CheckHealth"),
                ("DISM ScanHealth", "dism.exe", "/Online /Cleanup-Image /ScanHealth"),
                ("DISM RestoreHealth", "dism.exe", "/Online /Cleanup-Image /RestoreHealth"),
                ("SFC scan", "sfc.exe", "/scannow"),
                ("SFC confirm", "sfc.exe", "/scannow"),
            };
            int okCount = 0;
            foreach (var (label, file, args) in steps)
            {
                progress?.Invoke($"{label}…");
                var r = await RunAsync(file, args, 3600);
                log.AppendLine($"\r\n===== {label}: {file} {args} -> exit {r.Exit} =====\r\n{r.Full}");
                _log.Seal("repair", "repair-step", $"repair step {label}: exit {r.Exit}");
                if (r.Exit == 0) okCount++;
            }
            File.WriteAllText(logPath, log.ToString());
            _log.Seal("repair", "repair-run", $"repair run complete: {okCount}/{steps.Length} steps exit 0; log {logPath}");
            return (okCount == steps.Length, $"Repair run finished: {okCount}/{steps.Length} steps completed cleanly. Full log: {logPath}");
        }

        public async Task<(bool Ok, string Note)> RunQuickSfcAsync(Action<string>? progress = null)
        {
            if (!IsElevated())
                return (false, "SFC needs administrator. Run the app as administrator, or use the toolkit's Quick-SFC shortcut. Nothing was run.");
            progress?.Invoke("SFC /scannow…");
            var r = await RunAsync("sfc.exe", "/scannow", 3600);
            _log.Seal("repair", "sfc", $"quick SFC scan: exit {r.Exit}");
            return (r.Exit == 0, r.Exit == 0 ? "SFC completed — no integrity violations reported (exit 0)." : $"SFC finished with exit {r.Exit}. {r.Tail}");
        }

        private static async Task<(int Exit, string Tail, string Full)> RunScriptAsync(string script)
            => await RunAsync("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -File \"{script}\"", 300);

        private static async Task<(int Exit, string Tail, string Full)> RunAsync(string file, string args, int timeoutSec)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = file,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                };
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
            catch (Exception ex) { return (-2, ex.Message, ""); }
        }
    }
}
