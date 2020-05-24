using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace GhostOverlay
{
    public sealed partial class WidgetSettingsSettingsView : Page, ISubscriber<WidgetPropertyChanged>, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private readonly MyEventAggregator eventAggregator = new MyEventAggregator();

        private string _displayName;
        private string DisplayName
        {
            get => _displayName;
            set
            {
                _displayName = value;
                OnPropertyChanged();
            }
        }

        private string _platformString;
        private string PlatformString
        {
            get => _platformString;
            set
            {
                _platformString = value;
                OnPropertyChanged();
            }
        }

        private string _definitionsDbName;
        private string DefinitionsDbName
        {
            get => _definitionsDbName;
            set
            {
                _definitionsDbName = value;
                OnPropertyChanged();
            }
        }

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
            //Debug.WriteLine($"[WidgetSettingsSettingsView] HandleMessage {message}");

            switch (message)
            {
                case WidgetPropertyChanged.Profile:
                case WidgetPropertyChanged.DefinitionsPath:
                    UpdateViewModel();
                    break;
            }
        }

        private void UpdateViewModel()
        {
            if (AppState.Data.DefinitionsPath != null)
            {
                DefinitionsDbName = Path.GetFileName(AppState.Data.DefinitionsPath);
            }

            var userInfo = AppState.Data.Profile?.Profile?.Data?.UserInfo;
            if (userInfo != null)
            {
                DisplayName = userInfo.DisplayName;

                if (userInfo.CrossSaveOverride == 0)
                {
                    // User has not enabled cross save. Just one platform
                    PlatformString = $"({MembershipTypeToString(userInfo.MembershipType)})";
                }
                else
                {
                    var platforms = userInfo.ApplicableMembershipTypes.Select(MembershipTypeToString);
                    PlatformString = $"(Cross save: {string.Join(", ", platforms)})";
                }
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

        public static string MembershipTypeToString(int membershipType)
        {
            switch (membershipType)
            {
                case 0: return "None";
                case 1: return "Xbox";
                case 2: return "PlayStation";
                case 3: return "Steam";
                case 4: return "Blizzard";
                case 5: return "Stadia";
                case 10: return "Demon";
                case 254: return "BungieNext";
                case -1: return "None";
                default: return "Uknown";
            }
        }

        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
