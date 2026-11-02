using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Text.Json;
using System.Text.Json.Nodes;
using Capsule.Contracts;
using Microsoft.Win32;

namespace CapsuleEcology.Services
{
    /// <summary>
    /// Baseline snapshots (R3 — the P1/P2 debt the Capsule Order made
    /// committed): "observe before you modify" needs a recorded before.
    /// A baseline is a sealed JSON reading of the machine at a moment —
    /// OS build, hardware, disk, DNS, restore points, the tweak/pack
    /// catalog fingerprints, and the receipt chain's own length and
    /// verification state — so later states can be diffed against truth
    /// instead of memory. Baselines live in
    /// %LocalAppData%\CapsuleEcology\baselines\ and every capture seals
    /// a receipt (Order #3).
    /// </summary>
    public sealed class SystemSnapshotService
    {
        private readonly ReceiptLog _log;
        public SystemSnapshotService(ReceiptLog log) { _log = log; }

        public static string BaselineDir => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CapsuleEcology", "baselines");

        public (string Path, string Summary) Capture(RestorePointService restore, DnsService dns)
        {
            var root = new JsonObject();
            var now = DateTime.Now;
            root["format"] = "capsule-baseline/1";
            root["captured"] = now.ToString("yyyy-MM-dd HH:mm:ss");

            var os = new JsonObject();
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                os["product"] = key?.GetValue("ProductName")?.ToString() ?? "";
                os["displayVersion"] = key?.GetValue("DisplayVersion")?.ToString() ?? "";
                os["build"] = (key?.GetValue("CurrentBuild")?.ToString() ?? "") + "." + (key?.GetValue("UBR")?.ToString() ?? "");
            }
            catch { }
            os["machine"] = Environment.MachineName;
            os["osVersion"] = Environment.OSVersion.VersionString;
            os["processors"] = Environment.ProcessorCount;
            root["os"] = os;

            var hw = new JsonObject();
            try
            {
                using var cpu = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor");
                foreach (ManagementObject mo in cpu.Get()) hw["cpu"] = mo["Name"]?.ToString() ?? "";
                using var cs = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
                foreach (ManagementObject mo in cs.Get())
                    hw["ramGB"] = Math.Round(Convert.ToDouble(mo["TotalPhysicalMemory"]) / 1_073_741_824.0, 1);
            }
            catch { }
            root["hardware"] = hw;

            var disks = new JsonArray();
            foreach (var d in DriveInfo.GetDrives())
            {
                if (!d.IsReady) continue;
                disks.Add(new JsonObject
                {
                    ["drive"] = d.Name,
                    ["totalGB"] = Math.Round(d.TotalSize / 1_073_741_824.0, 1),
                    ["freeGB"] = Math.Round(d.AvailableFreeSpace / 1_073_741_824.0, 1),
                });
            }
            root["disks"] = disks;

            var dnsArr = new JsonArray();
            foreach (var (adapter, servers) in dns.Current())
                dnsArr.Add(new JsonObject { ["adapter"] = adapter, ["servers"] = servers });
            root["dns"] = dnsArr;

            var points = restore.List();
            root["restorePoints"] = new JsonObject
            {
                ["count"] = points.Count,
                ["newest"] = points.Count > 0 ? points[0].CreatedText + " · " + points[0].Description : "",
            };

            root["catalogs"] = new JsonObject
            {
                ["tweaksFingerprint"] = Fingerprint("catalog", "tweaks.catalog.json"),
                ["packsFingerprint"] = Fingerprint("packs", "packs.catalog.json"),
            };
            root["receipts"] = new JsonObject
            {
                ["length"] = _log.Length,
                ["chainVerifies"] = _log.Verify(),
            };

            Directory.CreateDirectory(BaselineDir);
            var path = Path.Combine(BaselineDir, $"baseline-{now:yyyyMMdd-HHmmss}.json");
            File.WriteAllText(path, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            _log.Seal("baseline", "snapshot", $"baseline captured: {Path.GetFileName(path)}");
            var summary = $"Baseline sealed — {os["product"]} build {os["build"]}, {points.Count} restore points, " +
                          $"DNS on {dnsArr.Count} adapter(s), receipt chain {_log.Length} entries (verifies: {_log.Verify()}). Saved to {path}";
            return (path, summary);
        }

        private static string Fingerprint(params string[] assetParts)
        {
            try
            {
                var parts = new List<string> { AppContext.BaseDirectory, "Assets" };
                parts.AddRange(assetParts);
                var text = File.ReadAllText(Path.Combine(parts.ToArray()));
                return Fnv.Fnv1a(text) + "/" + text.Length;
            }
            catch { return "unavailable"; }
        }

        public List<string> ListBaselines()
        {
            var list = new List<string>();
            try
            {
                if (Directory.Exists(BaselineDir))
                    foreach (var f in Directory.GetFiles(BaselineDir, "baseline-*.json")) list.Add(Path.GetFileName(f));
            }
            catch { }
            list.Sort((a, b) => string.Compare(b, a, StringComparison.Ordinal));
            return list;
        }
    }
}
