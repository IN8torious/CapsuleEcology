using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CapsuleEcology.Services;

namespace CapsuleEcology.Views
{
    public sealed partial class AppsPage : Page
    {
        private List<Pack> _packs = new();
        private readonly ReceiptLog _log = new();
        private PackageService? _svc;
        private bool _hasWinget, _hasChoco;
        private Pack? _pack;

        public AppsPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            _packs = Data.LoadPacks();
            _svc = new PackageService(_log);
            PackList.ItemsSource = _packs;
            var (w, c) = await _svc.DetectSourcesAsync();
            _hasWinget = w; _hasChoco = c;
            int apps = _packs.Sum(p => p.Apps.Count);
            Summary.Text = $"Install a role, not an application: {_packs.Count} curated packs · {apps} apps. " +
                $"Sources on this machine — winget: {(w ? "available" : "NOT FOUND")}, Chocolatey: {(c ? "available" : "not installed (optional)")}. " +
                "Every install demands a restore point first, discloses its manifest, and seals one receipt per app. Capsule orchestrates; the sources fetch from the original vendors.";
            if (_packs.Count > 0) PackList.SelectedIndex = 0;
        }

        private void PackList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _pack = PackList.SelectedItem as Pack;
            if (_pack is null) return;
            AppsList.ItemsSource = _pack.Apps;
            AppsList.DispatcherQueue.TryEnqueue(() =>
            {
                AppsList.SelectedItems.Clear();
                for (int i = 0; i < _pack.Apps.Count; i++)
                    if (!_pack.Apps[i].Optional) AppsList.SelectedItems.Add(AppsList.Items[i]);
                RefreshPlan();
            });
        }

        private List<PackApp> Selected() => AppsList.SelectedItems.Cast<PackApp>().ToList();

        private void AppsList_SelectionChanged(object sender, SelectionChangedEventArgs e) => RefreshPlan();

        private void RefreshPlan()
        {
            if (_pack is null || _svc is null) return;
            PlanOut.Text = _svc.PlanText(_pack, Selected(), _hasWinget, _hasChoco);
        }

        private async void BtnInstall_Click(object sender, RoutedEventArgs e) => await Run(install: true);
        private async void BtnUninstall_Click(object sender, RoutedEventArgs e) => await Run(install: false);

        private async Task Run(bool install)
        {
            if (_svc is null || _pack is null) return;
            var sel = Selected();
            if (sel.Count == 0) { Status.Text = "Nothing selected."; return; }
            BtnInstall.IsEnabled = BtnUninstall.IsEnabled = false;
            try
            {
                var results = install
                    ? await _svc.InstallAsync(_pack, sel, msg => DispatcherQueue.TryEnqueue(() => Status.Text = msg))
                    : await _svc.UninstallAsync(_pack, sel, msg => DispatcherQueue.TryEnqueue(() => Status.Text = msg));
                int ok = results.Count(r => r.Ok);
                var sb = new StringBuilder($"{(install ? "Installed" : "Uninstalled")} {ok}/{results.Count} apps from {_pack.Name}.");
                foreach (var r in results.Where(r => !r.Ok)) sb.Append($" Failed: {r.Description} — {r.Note}.");
                sb.Append(" Receipts sealed — see the Receipts page.");
                Status.Text = sb.ToString();
            }
            finally
            {
                BtnInstall.IsEnabled = BtnUninstall.IsEnabled = true;
            }
        }
    }
}
