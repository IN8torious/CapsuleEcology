using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CapsuleEcology.Services;

namespace CapsuleEcology.Views
{
    public sealed partial class UtilitiesPage : Page
    {
        private readonly ReceiptLog _log = new();
        private DnsService? _dns;
        private CleanerService? _cleaner;
        private List<CleanCategory> _cats = new();

        public UtilitiesPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            _dns = new DnsService(_log);
            _cleaner = new CleanerService(_log);
            Summary.Text = "The Sparkle-confirmed bench, under Capsule law: current state shown before any change, files only in the Cleaner, one-tap DNS revert to DHCP, and a receipt for every change.";
            DnsList.ItemsSource = DnsService.Providers;
            DnsList.SelectedIndex = 0;
            RefreshDns();
            await MeasureAsync();
        }

        private void RefreshDns()
        {
            if (_dns is null) return;
            var cur = _dns.Current();
            DnsCurrent.Text = cur.Count == 0
                ? "Current DNS: could not be read."
                : "Current DNS — " + string.Join(" · ", cur.Select(c => $"{c.Adapter}: {c.Servers}"));
        }

        private async void BtnDnsApply_Click(object sender, RoutedEventArgs e)
        {
            if (_dns is null || DnsList.SelectedItem is not DnsProvider provider) return;
            BtnDnsApply.IsEnabled = false;
            try
            {
                var r = await _dns.SetAsync(provider);
                DnsStatus.Text = r.Note;
                RefreshDns();
            }
            finally { BtnDnsApply.IsEnabled = true; }
        }

        private async Task MeasureAsync()
        {
            if (_cleaner is null) return;
            CleanStatus.Text = "Measuring — sizes first, always…";
            var raw = _cleaner.Categories();
            _cats = await Task.Run(() => raw.Select(c => _cleaner.Measure(c)).ToList());
            CleanList.ItemsSource = _cats;
            CleanList.DispatcherQueue.TryEnqueue(() =>
            {
                CleanList.SelectedItems.Clear();
                for (int i = 0; i < _cats.Count; i++)
                    if (_cats[i].Id is "temp" or "thumbnails") CleanList.SelectedItems.Add(CleanList.Items[i]);
            });
            CleanStatus.Text = "Measured. Pick categories, then clean — the Recycle Bin stays unselected unless you choose it.";
        }

        private async void BtnMeasure_Click(object sender, RoutedEventArgs e) => await MeasureAsync();

        private void BtnClean_Click(object sender, RoutedEventArgs e)
        {
            if (_cleaner is null) return;
            var sel = CleanList.SelectedItems.Cast<CleanCategory>().ToList();
            if (sel.Count == 0) { CleanStatus.Text = "Nothing selected."; return; }
            long freed = 0; int deleted = 0, skipped = 0;
            foreach (var cat in sel)
            {
                var r = _cleaner.Clean(cat);
                freed += r.Freed; deleted += r.Deleted; skipped += r.Skipped;
            }
            CleanStatus.Text = $"Cleaned {sel.Count} categories — {deleted} files removed, {freed / 1_000_000.0:0.0} MB freed where measurable, {skipped} skipped. Receipts sealed.";
            _ = MeasureAsync();
        }

        private void Launch(string file, string args = "")
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = file, Arguments = args, UseShellExecute = true });
                ToolStatus.Text = $"Opened {file}.";
            }
            catch (Exception ex) { ToolStatus.Text = $"Could not open {file}: {ex.Message}"; }
        }

        private void ToolCleanmgr_Click(object sender, RoutedEventArgs e) => Launch("cleanmgr.exe");
        private void ToolMsinfo_Click(object sender, RoutedEventArgs e) => Launch("msinfo32.exe");
        private void ToolStorage_Click(object sender, RoutedEventArgs e) => Launch("ms-settings:storagesense");

        private void SetPowerPlan(string guid, string name)
        {
            try
            {
                using var p = Process.Start(new ProcessStartInfo
                {
                    FileName = "powercfg.exe", Arguments = $"/setactive {guid}",
                    UseShellExecute = false, CreateNoWindow = true,
                });
                p?.WaitForExit(5000);
                ToolStatus.Text = p is not null && p.ExitCode == 0
                    ? $"Power plan set to {name}."
                    : $"Windows refused the {name} plan (it may not exist on this machine).";
                if (p is not null && p.ExitCode == 0)
                    _log.Seal("utilities", "power-plan", $"power plan set: {name} ({guid})");
            }
            catch (Exception ex) { ToolStatus.Text = $"Power plan change failed: {ex.Message}"; }
        }

        private void ToolPowerBalanced_Click(object sender, RoutedEventArgs e)
            => SetPowerPlan("381b4222-f694-41f0-9685-ff5bb260df2e", "Balanced");
        private void ToolPowerHigh_Click(object sender, RoutedEventArgs e)
            => SetPowerPlan("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c", "High performance");
    }
}
