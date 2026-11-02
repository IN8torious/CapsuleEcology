using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace CapsuleEcology.Views
{
    public sealed partial class SoonPage : Page
    {
        private static readonly Dictionary<string, (string Title, string Body)> Map = new()
        {
            ["lineage"] = ("Lineage", "The living tree arrives with the R4–R8 lineage work. The card schema and chain underneath are already verified (R0)."),
            ["agents"] = ("Agents & Crews", "Citizens and the Gate arrive in R7. D7 stands: humans are sources of authority; systems never are."),
            ["capsules"] = ("Capsules & Worlds", "The voxel editor, mod forge, and sealed scene capsules arrive in R5–R6. The .capsule exchange format is drafted with the contracts."),
            ["bible"] = ("Bible", "The reader arrives in R3. The text is ready: the Toolbox Bible JSONs — 66 books, 1,189 chapters, 31,098 verses, verified."),
            ["repair"] = ("Repair", "The repair face arrives in R3 on top of the shipped 0.2.2 toolkit: DISM CheckHealth → ScanHealth → RestoreHealth → SFC, with dated logs."),
            ["pages"] = ("All Pages", "The atlas view arrives as the remaining sections land."),
        };

        public SoonPage() { InitializeComponent(); }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var tag = e.Parameter as string ?? "";
            if (Map.TryGetValue(tag, out var info)) { Title.Text = info.Title; Body.Text = info.Body; }
            else { Title.Text = "Capsule Ecology"; Body.Text = "This section is on the roadmap."; }
        }
    }
}
