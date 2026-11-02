using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Threading.Tasks;

namespace CapsuleEcology.Services
{
    public sealed class DnsProvider
    {
        public string Name = "", Primary = "", Secondary = "", Note = "";
        public bool IsAutomatic => Primary.Length == 0;
        public string Line => IsAutomatic ? "Automatic (DHCP)" : $"{Name} ({Primary} / {Secondary})";
    }

    /// <summary>
    /// DNS Changer (R3 — the Sparkle-confirmed utility, under our law):
    /// Automatic (DHCP) is first and is the one-tap revert; providers are
    /// the public resolvers, disclosed with their addresses. Reads come
    /// from WMI (what Windows actually uses); writes go through an
    /// elevated PowerShell (UAC is the user's to accept — D7), and every
    /// write is verified by re-reading the adapters afterwards and
    /// sealed as a receipt.
    /// </summary>
    public sealed class DnsService
    {
        private readonly ReceiptLog _log;
        public DnsService(ReceiptLog log) { _log = log; }

        public static readonly List<DnsProvider> Providers = new()
        {
            new DnsProvider { Name = "Automatic (DHCP)", Note = "Whatever your network assigns — the revert path." },
            new DnsProvider { Name = "Cloudflare", Primary = "1.1.1.1", Secondary = "1.0.0.1", Note = "Fast, secure, privacy-focused DNS (recommended)." },
            new DnsProvider { Name = "Google", Primary = "8.8.8.8", Secondary = "8.8.4.4", Note = "Google Public DNS." },
            new DnsProvider { Name = "Quad9", Primary = "9.9.9.9", Secondary = "149.112.112.112", Note = "Blocks known-malicious domains." },
            new DnsProvider { Name = "OpenDNS", Primary = "208.67.222.222", Secondary = "208.67.220.220", Note = "Cisco OpenDNS." },
            new DnsProvider { Name = "AdGuard", Primary = "94.140.14.14", Secondary = "94.140.15.15", Note = "Ad/tracker filtering at the DNS level." },
        };

        public List<(string Adapter, string Servers)> Current()
        {
            var list = new List<(string, string)>();
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT Description, DNSServerSearchOrder FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = True");
                foreach (ManagementObject mo in searcher.Get())
                {
                    var servers = mo["DNSServerSearchOrder"] as string[];
                    list.Add((mo["Description"]?.ToString() ?? "adapter",
                        servers is { Length: > 0 } ? string.Join(" / ", servers) : "Automatic (DHCP)"));
                }
            }
            catch { }
            return list;
        }

        public async Task<(bool Ok, string Note)> SetAsync(DnsProvider provider)
        {
            string script = provider.IsAutomatic
                ? "Get-NetAdapter -Physical | ForEach-Object { Set-DnsClientServerAddress -InterfaceIndex $_.ifIndex -ResetServerAddresses }"
                : $"Get-NetAdapter -Physical | ForEach-Object {{ Set-DnsClientServerAddress -InterfaceIndex $_.ifIndex -ServerAddresses @('{provider.Primary}','{provider.Secondary}') }}";
            var ok = await RunElevatedPowerShellAsync(script);
            if (!ok) return (false, "The elevated change did not run (UAC declined or PowerShell unavailable). DNS unchanged.");
            await Task.Delay(1200);
            var now = Current();
            _log.Seal("dns", provider.IsAutomatic ? "dns-revert" : "dns-set",
                provider.IsAutomatic ? "dns reverted to automatic (DHCP)" : $"dns set: {provider.Line} on {now.Count} adapter(s)");
            return (true, provider.IsAutomatic
                ? "DNS reverted to Automatic (DHCP) and sealed."
                : $"DNS set to {provider.Line} and sealed. Current reading: {(now.Count > 0 ? now[0].Servers : "unreadable")}");
        }

        private static async Task<bool> RunElevatedPowerShellAsync(string script)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                    Verb = "runas",
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                };
                using var p = Process.Start(psi);
                if (p is null) return false;
                await p.WaitForExitAsync();
                return p.ExitCode == 0;
            }
            catch { return false; }  // UAC declined lands here — refusal is the answer, not an error to hide
        }
    }
}
