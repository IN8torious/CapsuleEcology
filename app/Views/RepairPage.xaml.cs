using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CapsuleEcology.Services;

namespace CapsuleEcology.Views
{
    public sealed partial class RepairPage : Page
    {
        private readonly ReceiptLog _log = new();
        private RepairService? _repair;
        private RestorePointService? _restore;
        private DnsService? _dns;
        private SystemSnapshotService? _snap;

        public RepairPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _repair = new RepairService(_log);
            _restore = new RestorePointService(_log);
            _dns = new DnsService(_log);
            _snap = new SystemSnapshotService(_log);
            Summary.Text = $"Capsule Order #2 and #4, made visible: recovery before change, a return path for everything. " +
                $"This app is {(RepairService.IsElevated() ? "running as administrator — repair runs are available" : "NOT elevated — installs and restore points work, DISM/SFC runs will refuse plainly (the toolkit shortcut self-elevates)")}.";
            RefreshToolkit();
            RefreshPoints();
            RefreshBaselines();
        }

        private void RefreshToolkit()
        {
            if (_repair is null) return;
            ToolkitStatus.Text = _repair.ToolkitInstalled
                ? $"Installed at {RepairService.ToolkitDir} — 'Repair Windows' is on the desktop and in the Start menu."
                : "Not installed. The toolkit ships with this app; installing puts the repair scripts and shortcuts in place.";
        }

        private void RefreshPoints()
        {
            if (_restore is null) return;
            PointList.ItemsSource = _restore.List();
        }

        private void RefreshBaselines()
        {
            if (_snap is null) return;
            BaselineList.ItemsSource = _snap.ListBaselines();
        }

        private async void BtnInstallToolkit_Click(object sender, RoutedEventArgs e)
        {
            if (_repair is null) return;
            BtnInstallToolkit.IsEnabled = false;
            try { var r = await _repair.InstallToolkitAsync(); ToolkitStatus.Text = r.Note; RefreshToolkit(); }
            finally { BtnInstallToolkit.IsEnabled = true; }
        }

        private async void BtnUninstallToolkit_Click(object sender, RoutedEventArgs e)
        {
            if (_repair is null) return;
            BtnUninstallToolkit.IsEnabled = false;
            try { var r = await _repair.UninstallToolkitAsync(); ToolkitStatus.Text = r.Note; RefreshToolkit(); }
            finally { BtnUninstallToolkit.IsEnabled = true; }
        }

        private async void BtnFullRepair_Click(object sender, RoutedEventArgs e) => await RunRepair(full: true);
        private async void BtnQuickSfc_Click(object sender, RoutedEventArgs e) => await RunRepair(full: false);

        private async Task RunRepair(bool full)
        {
            if (_repair is null) return;
            BtnFullRepair.IsEnabled = BtnQuickSfc.IsEnabled = false;
            try
            {
                var r = full
                    ? await _repair.RunFullRepairAsync(msg => DispatcherQueue.TryEnqueue(() => RepairStatus.Text = msg))
                    : await _repair.RunQuickSfcAsync(msg => DispatcherQueue.TryEnqueue(() => RepairStatus.Text = msg));
                RepairStatus.Text = r.Note;
            }
            finally { BtnFullRepair.IsEnabled = BtnQuickSfc.IsEnabled = true; }
        }

        private void BtnRefreshPoints_Click(object sender, RoutedEventArgs e) => RefreshPoints();

        private void BtnCreatePoint_Click(object sender, RoutedEventArgs e)
        {
            if (_restore is null) return;
            var ok = _restore.Create($"Capsule Ecology — manual point {DateTime.Now:yyyy-MM-dd HH:mm}", out var note);
            PointStatus.Text = note;
            if (ok) RefreshPoints();
        }

        private void BtnBaseline_Click(object sender, RoutedEventArgs e)
        {
            if (_snap is null || _restore is null || _dns is null) return;
            var (_, summary) = _snap.Capture(_restore, _dns);
            BaselineStatus.Text = summary;
            RefreshBaselines();
        }
    }
}
