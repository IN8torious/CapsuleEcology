using System;
using System.Collections.Generic;
using System.Management;

namespace CapsuleEcology.Services
{
    public sealed class RestorePointInfo
    {
        public ulong Sequence;
        public string Description = "", CreatedText = "", TypeText = "";
        public string Line => $"#{Sequence} · {CreatedText} · {Description}";
    }

    /// <summary>
    /// The restore-point manager (R3, Capsule Order #2/#4 made visible):
    /// list the points Windows actually holds, create new ones on demand.
    /// Creation shares TweakEngine's WMI contract (root\default
    /// SystemRestore.CreateRestorePoint) but without the session cache —
    /// a manager must be able to make more than one.
    /// </summary>
    public sealed class RestorePointService
    {
        private readonly ReceiptLog _log;
        public RestorePointService(ReceiptLog log) { _log = log; }

        public List<RestorePointInfo> List()
        {
            var points = new List<RestorePointInfo>();
            try
            {
                using var searcher = new ManagementObjectSearcher(@"root\default", "SELECT * FROM SystemRestore");
                foreach (ManagementObject mo in searcher.Get())
                {
                    var created = "";
                    try { created = ManagementDateTimeConverter.ToDateTime(mo["CreationTime"]?.ToString() ?? "").ToString("yyyy-MM-dd HH:mm"); }
                    catch { created = mo["CreationTime"]?.ToString() ?? ""; }
                    points.Add(new RestorePointInfo
                    {
                        Sequence = Convert.ToUInt64(mo["SequenceNumber"] ?? 0),
                        Description = mo["Description"]?.ToString() ?? "",
                        CreatedText = created,
                        TypeText = mo["RestorePointType"]?.ToString() ?? "",
                    });
                }
            }
            catch { /* listing is best-effort; the page reports the empty truth */ }
            points.Sort((a, b) => b.Sequence.CompareTo(a.Sequence));
            return points;
        }

        public bool Create(string description, out string note)
        {
            try
            {
                using var mc = new ManagementClass(@"root\default", "SystemRestore", null);
                var args = mc.GetMethodParameters("CreateRestorePoint");
                args["Description"] = description;
                args["RestorePointType"] = 12;   // MODIFY_SETTINGS
                args["EventType"] = 100;         // BEGIN_SYSTEM_CHANGE
                var result = mc.InvokeMethod("CreateRestorePoint", args, null);
                var code = Convert.ToUInt32(result?["ReturnValue"] ?? 1);
                if (code == 0)
                {
                    _log.Seal("restore-points", "restore-point", $"restore point created: {description}");
                    note = "Restore point created and sealed as a receipt.";
                    return true;
                }
                note = $"Windows refused the restore point (code {code}). Nothing was created.";
                return false;
            }
            catch (Exception ex)
            {
                note = $"Restore point unavailable: {ex.Message}";
                return false;
            }
        }
    }
}
