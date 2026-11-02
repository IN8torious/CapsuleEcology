using System.Collections.Generic;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Capsule.Contracts;

namespace CapsuleEcology.Views
{
    public sealed partial class ReceiptsPage : Page
    {
        private readonly Ledger _ledger = new();
        private bool _tampered;

        private static readonly (string Agent, string Kind, string Event)[] Seed =
        {
            ("genesis-smith", "create", "N8-Core"),
            ("nate", "fork", "N8-Core-Frost"),
            ("vera", "mutate", "Frost-X: reasoning+2"),
            ("genesis-smith", "create", "PoeTchi-Delta"),
            ("nate", "breed", "Temple-Memory x Frost-X"),
        };

        public ReceiptsPage()
        {
            InitializeComponent();
            Build();
        }

        private void Build()
        {
            _ledger.Clear();
            foreach (var (a, k, ev) in Seed) _ledger.Seal(a, k, ev);
            Refresh();
        }

        private void Refresh()
        {
            EntryList.ItemsSource = null;
            EntryList.ItemsSource = new List<LedgerEntry>(_ledger.Entries);
            VerifyOut.Text = "";
        }

        private void BtnVerify_Click(object sender, RoutedEventArgs e)
        {
            bool ok = _ledger.Verify();
            VerifyOut.Text = ok ? "✓ Chain verified." : "✗ VERIFICATION FAILED — history was rewritten; the chain refuses it.";
            VerifyOut.Foreground = new SolidColorBrush(ok ? Colors.MediumSeaGreen : Colors.IndianRed);
        }

        private void BtnTamper_Click(object sender, RoutedEventArgs e)
        {
            _tampered = !_tampered;
            BtnTamper.Content = _tampered ? "Restore entry 3" : "Tamper with entry 3";
            // Post-seal edit of the stored entry — its hash is NOT recomputed.
            // That is what tampering means, and Verify() must refuse it.
            if (_ledger.Entries.Count > 2)
                _ledger.Entries[2].Event = _tampered ? Seed[2].Event + " (edited)" : Seed[2].Event;
            Refresh();
        }
    }
}
