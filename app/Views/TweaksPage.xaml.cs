using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CapsuleEcology.Services;

namespace CapsuleEcology.Views
{
    public sealed partial class TweaksPage : Page
    {
        private List<FullOption> _all = new();
        private readonly ReceiptLog _log = new();
        private TweakEngine? _engine;

        public TweaksPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _all = Data.LoadCatalogFull();
            _engine = new TweakEngine(_log);
            int actions = _all.Sum(o => o.Apply.Count);
            Summary.Text = $"The real 0.2.2 catalog: {_all.Count} options · {actions} apply actions. Applying demands a restore point first, and every option seals a receipt (log: {_log.Length} entries, self-verified on load). HKLM options need the app running elevated.";
            OptionList.ItemsSource = _all;
        }

        private List<FullOption> Selected() =>
            OptionList.SelectedItems.Cast<FullOption>().ToList();

        private void Filter_TextChanged(object sender, TextChangedEventArgs e)
        {
            var f = Filter.Text.Trim().ToLowerInvariant();
            OptionList.ItemsSource = string.IsNullOrEmpty(f)
                ? _all
                : _all.Where(o => o.Text.ToLowerInvariant().Contains(f) || o.Id.Contains(f)).ToList();
        }

        private void OptionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var sel = Selected();
            if (sel.Count == 0)
            {
                PlanOut.Text = "Select options to see the exact plan — every action, in order, before anything is touched.";
                return;
            }
            var sb = new StringBuilder();
            int total = 0;
            foreach (var o in sel)
            {
                var steps = TweakEngine.Plan(o, revert: false);
                total += steps.Count;
                sb.AppendLine($"— {o.Text} ({o.Id}): {steps.Count} actions");
                foreach (var s in steps.Take(6)) sb.AppendLine("  " + s);
                if (steps.Count > 6) sb.AppendLine($"  … and {steps.Count - 6} more");
                sb.AppendLine();
            }
            sb.AppendLine($"TOTAL: {total} actions across {sel.Count} options. A restore point is created before the first change.");
            PlanOut.Text = sb.ToString();
        }

        private void Run(bool revert)
        {
            if (_engine is null) return;
            var sel = Selected();
            if (sel.Count == 0) { Status.Text = "Nothing selected."; return; }
            if (!revert && !_engine.EnsureRestorePoint("Capsule Ecology — before tweak apply", out var note))
            {
                Status.Text = note;
                return;
            }
            int ok = 0, total = 0;
            foreach (var o in sel)
            {
                var res = _engine.Apply(o, revert);
                ok += res.FindAll(r => r.Ok).Count;
                total += res.Count;
            }
            Status.Text = $"{(revert ? "Reverted" : "Applied")}: {ok}/{total} actions succeeded. Receipts sealed — see the Receipts page lineage in %LocalAppData%\\CapsuleEcology\\receipts.jsonl.";
        }

        private void BtnApply_Click(object sender, RoutedEventArgs e) => Run(false);
        private void BtnRevert_Click(object sender, RoutedEventArgs e) => Run(true);
    }
}
