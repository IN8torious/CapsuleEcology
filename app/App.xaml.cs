using Microsoft.UI.Xaml;

namespace CapsuleEcology
{
    public partial class App : Application
    {
        public App() { InitializeComponent(); }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            WindowRef = _window;
            _window.Activate();
        }

        private Window? _window;

        /// <summary>The main window, for ThemeService.ApplyToWindow.</summary>
        public static Window? WindowRef { get; private set; }
    }
}
