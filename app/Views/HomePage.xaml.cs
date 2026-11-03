using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Capsule.Contracts;
using CapsuleEcology.Services;

namespace CapsuleEcology.Views
{
    public sealed partial class HomePage : Page
    {
        public HomePage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void Appearance_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button b) return;
            ThemeService.SetAppearance(b.Tag?.ToString() ?? ThemeService.Obsidian);
            ThemeService.ApplyToWindow();
            ThemeStatus.Text = $"Now: {ThemeService.Appearance} · accent {ThemeService.Accent} — saved as your choice.";
        }

        private void Accent_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button b) return;
            ThemeService.SetAccent(b.Tag?.ToString() ?? ThemeService.DefaultAccent);
            ThemeService.ApplyToWindow();
            ThemeStatus.Text = $"Now: {ThemeService.Appearance} · accent {ThemeService.Accent} — saved as your choice.";
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ThemeService.Load();
            ThemeStatus.Text = $"Now: {ThemeService.Appearance} · accent {ThemeService.Accent}.";
            var cards = Data.LoadCards();
            StatModels.Text = $"{cards.Count} adopted";
            var (options, apply, _, _) = Data.LoadCatalog();
            StatTweaks.Text = $"{options} options · {apply} actions";

            var ledger = new Ledger();
            ledger.Seal("genesis-smith", "create", "N8-Core");
            ledger.Seal("nate", "fork", "N8-Core-Frost");
            ledger.Seal("vera", "mutate", "Frost-X: reasoning+2");
            ledger.Seal("genesis-smith", "create", "PoeTchi-Delta");
            ledger.Seal("nate", "breed", "Temple-Memory x Frost-X");
            foreach (var entry in ledger.Entries)
            {
                var row = new Grid();
                row.ColumnDefinitions.Add(new ColumnDefinition());
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                var left = new TextBlock { Text = $"#{entry.Seq} {entry.Kind} — {entry.Event}", Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Silver) };
                var right = new TextBlock { Text = entry.Hash, FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"), Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 217, 164, 65)) };
                Grid.SetColumn(right, 1);
                row.Children.Add(left); row.Children.Add(right);
                ReceiptList.Children.Add(row);
            }
        }
    }
}
