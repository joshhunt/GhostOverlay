using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace GhostOverlay
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class WidgetSettingsSettingsView : Page, ISubscriber<WidgetPropertyChanged>
    {
        private string definitionsDbName = "<not loaded>";
        private readonly MyEventAggregator eventAggregator = new MyEventAggregator();

        public WidgetSettingsSettingsView()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            eventAggregator.Subscribe(this);
            UpdateViewModel();
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            eventAggregator.Unsubscribe(this);
        }

        public void HandleMessage(WidgetPropertyChanged message)
        {
            Debug.WriteLine($"[WidgetSettingsSettingsView] HandleMessage {message}");

            switch (message)
            {
                case WidgetPropertyChanged.DefinitionsPath:
                    UpdateViewModel();
                    break;
            }
        }

        private void UpdateViewModel()
        {
            if (AppState.Data.DefinitionsPath != null)
            {
                definitionsDbName = Path.GetFileName(AppState.Data.DefinitionsPath);
            }
        }

        private async void UpdateDefinitionsButton_OnClick(object sender, RoutedEventArgs e)
        {
            DefinitionsProgressRing.IsActive = true;
            var rateLimit = Task.Delay(TimeSpan.FromSeconds(2));
            await Definitions.CheckForLatestDefinitions();
            await rateLimit;
            DefinitionsProgressRing.IsActive = false;
        }

        private void SignOutConfirmedClicked(object sender, RoutedEventArgs e)
        {
            AppState.Data.SignOutAndResetAllData();
        }
    }
}
