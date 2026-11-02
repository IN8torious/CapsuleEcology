using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CapsuleEcology.Views;

namespace CapsuleEcology
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            ContentFrame.Navigate(typeof(HomePage));
        }

        private void Nav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is not NavigationViewItem item) return;
            var tag = item.Tag?.ToString() ?? "home";
            Type page = tag switch
            {
                "home" => typeof(HomePage),
                "models" => typeof(ModelsPage),
                "receipts" => typeof(ReceiptsPage),
                "tweaks" => typeof(TweaksPage),
                "apps" => typeof(AppsPage),
                "utilities" => typeof(UtilitiesPage),
                "repair" => typeof(RepairPage),
                "bible" => typeof(BiblePage),
                _ => typeof(SoonPage),
            };
            if (ContentFrame.CurrentSourcePageType != page)
                ContentFrame.Navigate(page, tag);
        }
    }
}
