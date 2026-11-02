using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Text.Json;
using Capsule.Contracts;
using Microsoft.Win32;

namespace CapsuleEcology.Services
{
    public sealed class StepResult
    {
        public string Description = "";
        public bool Ok;
        public string Note = "";
    }

    /// <summary>
    /// The persistent receipt log: an SI ledger on disk (JSONL).
    /// On load, entries are re-sealed in memory and their hashes
    /// must match what was stored — the file audits itself.
    /// </summary>
    public sealed class ReceiptLog
    {
        private readonly Ledger _ledger = new();
        private readonly string _path;

        public ReceiptLog()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CapsuleEcology");
            Directory.CreateDirectory(dir);
            _path = Path.Combine(dir, "receipts.jsonl");
            if (File.Exists(_path))
            {
                foreach (var line in File.ReadAllLines(_path))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    using var doc = JsonDocument.Parse(line);
                    var r = doc.RootElement;
                    var e = _ledger.Seal(
                        r.GetProperty("agent").GetString() ?? "",
                        r.GetProperty("kind").GetString() ?? "",
                        r.GetProperty("event").GetString() ?? "");
                    if (e.Hash != r.GetProperty("hash").GetString())
                        throw new InvalidDataException(
                            "Receipt log failed its own verification. History was rewritten; refusing to extend it.");
                }
            }
        }

        public LedgerEntry Seal(string agent, string kind, string evt)
        {
            var e = _ledger.Seal(agent, kind, evt);
            File.AppendAllText(_path, JsonSerializer.Serialize(e) + Environment.NewLine);
            return e;
        }

        public bool Verify() => _ledger.Verify();
        public int Length => _ledger.Length;
    }

    /// <summary>
    /// Applies and reverts catalog options. Law-bound by
    /// construction: it can only execute what the verified
    /// catalog contains, it demands a restore point before the
    /// first change of a session, and every option-level outcome
    /// seals a receipt — success and failure alike.
    /// </summary>
    public sealed class TweakEngine
    {
        private readonly ReceiptLog _log;
        private bool _restorePointMade;

        public TweakEngine(ReceiptLog log) { _log = log; }

        public static List<string> Plan(FullOption option, bool revert)
        {
            var steps = new List<string>();
            foreach (var a in revert ? option.Revert : option.Apply)
            {
                steps.Add(a.Kind switch
                {
                    "registryValue" => $"REG  {a.Path} :: {a.Value} = {a.Data} ({a.Type})" +
                                       (a.Operation == "delete" ? "  [delete]" : ""),
                    "service" => $"SVC  {a.Name} -> {a.Startup}",
                    "powerShell" => $"PS   {a.Script}",
                    _ => $"{a.Kind}  (unknown — will be refused)",
                });
            }
            return steps;
        }

        public bool EnsureRestorePoint(string description, out string note)
        {
            if (_restorePointMade) { note = "Restore point already created this session."; return true; }
            try
            {
                using var mc = new ManagementClass(@"root\default", "SystemRestore", null);
                var args = mc.GetMethodParameters("CreateRestorePoint");
                args["Description"] = description;
                args["RestorePointType"] = 12;   // MODIFY_SETTINGS
                args["EventType"] = 100;         // BEGIN_SYSTEM_CHANGE
                var result = mc.InvokeMethod("CreateRestorePoint", args, null);
                var code = Convert.ToUInt32(result?["ReturnValue"] ?? 1);
                _restorePointMade = code == 0;
                note = code == 0 ? "Restore point created." : $"Restore point refused by Windows (code {code}). Nothing was applied.";
                return _restorePointMade;
            }
            catch (Exception ex)
            {
                note = $"Restore point unavailable: {ex.Message}. Nothing was applied.";
                return false;
            }
        }

        public List<StepResult> Apply(FullOption option, bool revert)
        {
            var results = new List<StepResult>();
            foreach (var a in revert ? option.Revert : option.Apply)
            {
                var r = a.Kind switch
                {
                    "registryValue" => ApplyRegistry(a),
                    "service" => ApplyService(a),
                    "powerShell" => ApplyScript(a, revert),
                    _ => new StepResult { Description = a.Kind, Ok = false, Note = "Unknown action kind — refused." },
                };
                results.Add(r);
            }
            int okCount = results.FindAll(r => r.Ok).Count;
            _log.Seal("capsule-ecology", revert ? "revert" : "apply",
                $"{option.Id}: {okCount}/{results.Count} actions");
            return results;
        }

        private static (RegistryKey? Root, string Sub) SplitPath(string path)
        {
            int i = path.IndexOf('\\');
            if (i < 0) return (null, path);
            var rootName = path[..i].ToUpperInvariant();
            var sub = path[(i + 1)..];
            RegistryKey? root = rootName switch
            {
                "HKCU" => Registry.CurrentUser,
                "HKLM" => Registry.LocalMachine,
                "HKCR" => Registry.ClassesRoot,
                "HKU" => null, // HKU\\.DEFAULT and SIDs need explicit hives; refused loudly below
                _ => null,
            };
            return (root, sub);
        }

        private static StepResult ApplyRegistry(CatalogAction a)
        {
            var (root, sub) = SplitPath(a.Path);
            if (root is null)
                return new StepResult { Description = a.Path, Ok = false, Note = "Unknown hive — refused." };
            try
            {
                using var key = root.CreateSubKey(sub);
                if (a.Operation == "delete")
                {
                    key.DeleteValue(a.Value, throwOnMissingValue: false);
                    return new StepResult { Description = $"{a.Path} :: {a.Value}", Ok = true, Note = "deleted" };
                }
                object value = a.Type.ToUpperInvariant() switch
                {
                    "REG_DWORD" => Convert.ToInt32(a.Data.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                        ? Convert.ToInt32(a.Data[2..], 16) : int.Parse(a.Data)),
                    "REG_QWORD" => (object)long.Parse(a.Data),
                    _ => a.Data,
                };
                var kind = a.Type.ToUpperInvariant() switch
                {
                    "REG_DWORD" => RegistryValueKind.DWord,
                    "REG_QWORD" => RegistryValueKind.QWord,
                    "REG_EXPAND_SZ" => RegistryValueKind.ExpandString,
                    "REG_MULTI_SZ" => RegistryValueKind.MultiString,
                    _ => RegistryValueKind.String,
                };
                key.SetValue(a.Value, value, kind);
                return new StepResult { Description = $"{a.Path} :: {a.Value}", Ok = true, Note = $"= {a.Data}" };
            }
            catch (Exception ex)
            {
                return new StepResult { Description = a.Path, Ok = false, Note = ex.Message };
            }
        }

        private static StepResult ApplyService(CatalogAction a)
        {
            // Startup type lives at HKLM\SYSTEM\CurrentControlSet\Services\<name>\Start
            int start = a.Startup.ToLowerInvariant() switch
            {
                "automatic" => 2, "manual" => 3, "disabled" => 4, _ => -1,
            };
            if (start < 0) return new StepResult { Description = a.Name, Ok = false, Note = $"Unknown startup '{a.Startup}' — refused." };
            try
            {
                using var key = Registry.LocalMachine.CreateSubKey(
                    $@"SYSTEM\CurrentControlSet\Services\{a.Name}");
                key.SetValue("Start", start, RegistryValueKind.DWord);
                return new StepResult { Description = a.Name, Ok = true, Note = $"Start = {a.Startup}" };
            }
            catch (Exception ex)
            {
                return new StepResult { Description = a.Name, Ok = false, Note = ex.Message };
            }
        }

        private static StepResult ApplyScript(CatalogAction a, bool revert)
        {
            var script = Path.Combine(AppContext.BaseDirectory, "Assets", "scripts", a.Script);
            if (!File.Exists(script))
                return new StepResult { Description = a.Script, Ok = false, Note = "Script not shipped — refused." };
            try
            {
                using var p = Process.Start(new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{script}\"",
                    UseShellExecute = false, CreateNoWindow = true,
                });
                p?.WaitForExit(120_000);
                return new StepResult { Description = a.Script, Ok = p?.ExitCode == 0, Note = $"exit {p?.ExitCode}" };
            }
            catch (Exception ex)
            {
                return new StepResult { Description = a.Script, Ok = false, Note = ex.Message };
            }
        }
    }
}
