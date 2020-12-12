using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using GhostSharper.Models;

namespace GhostOverlay
{
    public sealed partial class WidgetSettingsSettingsView : Page, ISubscriber<WidgetPropertyChanged>, INotifyPropertyChanged
    {
#pragma warning disable 67
        public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore 67

        private readonly WidgetStateChangeNotifier eventAggregator = new WidgetStateChangeNotifier();
        private readonly Logger Log = new Logger("WidgetSettingsSettingsView");
        private readonly RangeObservableCollection<CoreSetting> Languages = new RangeObservableCollection<CoreSetting>();

        private string DisplayName { get; set; }
        private bool ShowDevOptions { get; set; }
        private string PlatformString { get; set; }
        private string DefinitionsDbName { get; set; }
        private CoreSetting SelectedLanguage { get; set; }

        public WidgetSettingsSettingsView()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            _ = AppState.Data.UpdateDestinySettings();
            eventAggregator.Subscribe(this);
            UpdateViewModel();
            UpdateSelectedLanguage();
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            eventAggregator.Unsubscribe(this);
        }

        public void HandleMessage(WidgetPropertyChanged message)
        {
            switch (message)
            {
                case WidgetPropertyChanged.Profile:
                case WidgetPropertyChanged.DefinitionsPath:
                case WidgetPropertyChanged.DestinySettings:
                case WidgetPropertyChanged.ShowDescriptions:
                case WidgetPropertyChanged.ShowDevOptions:
                    UpdateViewModel();
                    break;

                case WidgetPropertyChanged.Language:
                    UpdateSelectedLanguage();
                    break;
            }
        }

        private void UpdateSelectedLanguage()
        {
            SelectedLanguage = Languages.FirstOrDefault(v => v.Identifier == AppState.Data.Language.Value);
            Log.Info("SelectedLanguage {SelectedLanguage}", SelectedLanguage);
        }

        private void UpdateViewModel()
        {
            ShowDescriptionsCheckbox.IsChecked = AppState.Data.ShowDescriptions.Value;

            if (AppState.Data.DefinitionsPath != null)
            {
                DefinitionsDbName = Path.GetFileName(AppState.Data.DefinitionsPath);
            }

            ShowDevOptions = AppState.Data.ShowDevOptions.Value;

            if (AppState.Data.DestinySettings?.Value?.SystemContentLocales != null)
            {
                Languages.Clear();
                Languages.AddRange(AppState.Data.DestinySettings.Value.SystemContentLocales);

                UpdateSelectedLanguage();
            }

            var userInfo = AppState.Data.Profile?.Profile?.Data?.UserInfo;
            if (userInfo != null)
            {
                DisplayName = userInfo.DisplayName;

                if (userInfo.CrossSaveOverride == 0)
                {
                    // User has not enabled cross save. Just one platform
                    PlatformString = $"({userInfo.MembershipType})";
                }
                else
                {
                    var platforms = userInfo.ApplicableMembershipTypes.Select(v => v.ToString());
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
                case 2: return "PSN";
                case 3: return "Steam";
                case 4: return "Blizzard";
                case 5: return "Stadia";
                case 10: return "Demon";
                case 254: return "BungieNext";
                case -1: return "None";
                default: return "Uknown";
            }
        }

        private async void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is CoreSetting added && AppState.Data.Language.Value != added.Identifier)
            {
                Log.Info("Changed language to {added}", added.Identifier);
                LanguageDefinitionsProgressRing.IsActive = true;
                LanguageComboBox.IsEnabled = false;
                await AppState.Data.UpdateDefinitionsLanguage(added.Identifier);
                LanguageComboBox.IsEnabled = true;
                LanguageDefinitionsProgressRing.IsActive = false;
            }
        }

        private void ShowDescriptionsToggled(object sender, RoutedEventArgs e)
        {
            AppState.Data.ShowDescriptions.Value = ((CheckBox) e.OriginalSource)?.IsChecked ?? false;
            Log.Info("AppState.Data.ShowDescriptions.Value to {v}", AppState.Data.ShowDescriptions.Value);
        }

        private int tappedCount = 0;
        private void UIElement_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            tappedCount += 1;
            Log.Info("Dev tapped count {v}", tappedCount);

            if (tappedCount >= 10)
            {
                AppState.Data.ShowDevOptions.Value = !AppState.Data.ShowDevOptions.Value;
                Log.Info("  ShowDevOptions is now {v}", AppState.Data.ShowDevOptions.Value);
                tappedCount = 0;
            }
        }
    }
}
