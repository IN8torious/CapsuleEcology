using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CapsuleEcology.Services;

namespace CapsuleEcology.Views
{
    public sealed partial class ModelsPage : Page
    {
        private readonly ReceiptLog _log = new();
        private List<ModelEntry> _models = new();
        private ModelService? _svc;

        public ModelsPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            CardList.ItemsSource = Data.LoadCards();
            _models = Data.LoadModelCatalog();
            _svc = new ModelService(_log);
            ModelSel.ItemsSource = _models.Select(m => $"{m.Name} — {m.WeightsText}").ToList();
            if (_models.Count > 0) ModelSel.SelectedIndex = 0;
        }

        private ModelEntry? Current => ModelSel.SelectedIndex >= 0 ? _models[ModelSel.SelectedIndex] : null;

        private void ModelSel_SelectionChanged(object sender, SelectionChangedEventArgs e) => RefreshState();

        private void RefreshState()
        {
            if (_svc is null || Current is null) return;
            FileList.ItemsSource = Current.Files;
            ModelStatus.Text = $"{Current.Name} — {Current.SourceText} · state: {_svc.StateOf(Current)} · stored at {_svc.ModelDir(Current)}";
        }

        private async void BtnDownload_Click(object sender, RoutedEventArgs e)
        {
            if (_svc is null || Current is null) return;
            BtnDownload.IsEnabled = false;
            try
            {
                var r = await _svc.DownloadAsync(Current, msg => DispatcherQueue.TryEnqueue(() => ModelOut.Text = msg));
                ModelOut.Text = r.Note;
                RefreshState();
            }
            finally { BtnDownload.IsEnabled = true; }
        }

        private async void BtnVerify_Click(object sender, RoutedEventArgs e)
        {
            if (_svc is null || Current is null) return;
            BtnVerify.IsEnabled = false;
            try
            {
                var m = Current;
                var r = await Task.Run(() => _svc.Verify(m));
                ModelOut.Text = (r.Ok ? "Verified — every file matches the pinned source.\n" : "Verification FAILED — this model does not run and cannot be adopted.\n")
                    + string.Join("\n", r.Lines);
                RefreshState();
            }
            finally { BtnVerify.IsEnabled = true; }
        }

        private void BtnAdopt_Click(object sender, RoutedEventArgs e)
        {
            if (_svc is null || Current is null) return;
            var r = _svc.Adopt(Current);
            ModelOut.Text = r.Note;
            RefreshState();
        }

        private void BtnFork_Click(object sender, RoutedEventArgs e)
        {
            if (_svc is null || Current is null) return;
            var name = ForkName.Text.Trim();
            if (name.Length == 0) { ModelOut.Text = "Name the fork first — lineage starts with a name."; return; }
            var r = _svc.Fork(Current, name);
            ModelOut.Text = r.Note;
        }

        private void BtnMutate_Click(object sender, RoutedEventArgs e)
        {
            if (_svc is null) return;
            var forks = _svc.ListForks();
            if (forks.Count == 0) { ModelOut.Text = "No local forks yet — fork an adopted model first."; return; }
            var mutation = MutateText.Text.Trim();
            if (mutation.Length == 0) { ModelOut.Text = "Describe the mutation first."; return; }
            forks.Sort();
            var r = _svc.Mutate(forks[^1], mutation.Replace("\"", "'"));
            ModelOut.Text = r.Note;
        }
    }
}
