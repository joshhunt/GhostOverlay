using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Microsoft.Gaming.XboxGameBar;
using GhostOverlay.Views;
using NavigationViewItem = Microsoft.UI.Xaml.Controls.NavigationViewItem;
using NavigationView = Microsoft.UI.Xaml.Controls.NavigationView;
using NavigationViewBackRequestedEventArgs = Microsoft.UI.Xaml.Controls.NavigationViewBackRequestedEventArgs;
using NavigationViewSelectionChangedEventArgs = Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs;

namespace GhostOverlay
{
    public sealed partial class WidgetSettingsView : Page, ISubscriber<WidgetPropertyChanged>, INotifyPropertyChanged
    {
#pragma warning disable 67`
        public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore 67

        private XboxGameBarWidget widget;
        private readonly WidgetStateChangeNotifier eventAggregator = new WidgetStateChangeNotifier();
        private static readonly Logger Log = new Logger("WidgetSettingsView");

        private readonly ObservableCollection<TrackableOwner> Characters = new ObservableCollection<TrackableOwner>();

        private TrackableOwner ActiveCharacter { get; set; }
        private bool SeasonalChallengesVisible = false;
        private PresentationNode SeasonalChallengePresentationNode { get; set; }

        public WidgetSettingsView()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Log.Info("OnNavigatedTo");
            widget = e.Parameter as XboxGameBarWidget;

            if (widget != null)
            {
                Log.Info("widget not null, setting MaxWindowSize");
                widget.MaxWindowSize = new Size(2000, 2000);

                Log.Info("widget not null, setting MinWindowSize");
                widget.MinWindowSize = new Size(715, 200);

                Log.Info("widget not null, setting supported resize stuff");
                widget.HorizontalResizeSupported = true;
                widget.VerticalResizeSupported = true;

                Log.Info("widget not null, setting SettingsSupported");
                widget.SettingsSupported = false;
            }

            Log.Info("Done with Widget stuff, onto view init");

            NavView.SelectedItem = NavView.MenuItems[0];

            eventAggregator.Subscribe(this);
            AppState.Data.ScheduleProfileUpdates();

            UpdateCharacterList();
            UpdateActiveCharacter();
            UpdateSeasonalChallenges();

            Log.Info("OnNavigatedTo complete");
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            AppState.Data.UnscheduleProfileUpdates();
        }

        public void HandleMessage(WidgetPropertyChanged message)
        {
            switch (message)
            {
                case WidgetPropertyChanged.TokenData:
                    CheckAuth();
                    break;

                case WidgetPropertyChanged.Profile:
                case WidgetPropertyChanged.DefinitionsPath:
                    UpdateCharacterList();
                    break;

                case WidgetPropertyChanged.ActiveCharacter:
                    UpdateActiveCharacter();
                    break;

                case WidgetPropertyChanged.DestinySettings:
                    UpdateSeasonalChallenges();
                    break;
            }
        }

        private async void UpdateSeasonalChallenges()
        {   
            var seasonalChallengesRootHash = Convert.ToInt64(AppState.Data.DestinySettings?.Value?.Destiny2CoreSettings
                ?.SeasonalChallengesPresentationNodeHash);
            Log.Info(" UpdateSeasonalChallengesseasonalChallengesRootHash {seasonalChallengesRootHash}", seasonalChallengesRootHash);

            var parentChallengesNode = await PresentationNode.FromHash(seasonalChallengesRootHash, AppState.Data.Profile, null);
            var firstChild = parentChallengesNode.Definition?.Children?.PresentationNodes?.FirstOrDefault();

            var seasonalChallengesNode =
                await PresentationNode.FromHash(Convert.ToInt64(firstChild?.PresentationNodeHash), AppState.Data.Profile, parentChallengesNode);

            if (seasonalChallengesNode.Definition != null)
            {
                SeasonalChallengePresentationNode = seasonalChallengesNode;
                SeasonalChallengesVisible = true;
            }
        }

        private async void UpdateCharacterList()
        {
            var profile = AppState.Data.Profile;
            var charactersData = profile?.Characters.Data;
            if (charactersData == null) return;

            Characters.Clear();
            
            foreach (var destinyEntitiesCharactersDestinyCharacterComponent in charactersData.Values)
            {
                var newCharacter = new TrackableOwner
                {
                    CharacterComponent = destinyEntitiesCharactersDestinyCharacterComponent,
                };

                await newCharacter.PopulatedExtendedDefinitions();

                Characters.Add(newCharacter);
            }

            var activeCharacterId = AppState.Data.ActiveCharacter?.CharacterId;

            if (activeCharacterId == null)
            {
                DateTime activityStart = DateTime.MinValue;

                foreach (var (characterId, activitiesComponent) in profile.CharacterActivities.Data)
                {
                    if (activitiesComponent.CurrentActivityHash != 0 && activitiesComponent.DateActivityStarted > activityStart)
                    {
                        activeCharacterId = long.Parse(characterId);
                        activityStart = activitiesComponent.DateActivityStarted;
                    }
                }
            }

            AppState.Data.ActiveCharacter = Characters.FirstOrDefault(v => v.CharacterId == activeCharacterId) ?? Characters.First();
        }

        private void UpdateActiveCharacter()
        {
            if (ActiveCharacter != AppState.Data.ActiveCharacter)
                ActiveCharacter = AppState.Data.ActiveCharacter;
        }

        private void NavView_OnSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                ContentFrame.Navigate(typeof(WidgetSettingsSettingsView), null,
                    args.RecommendedNavigationTransitionInfo);
                return;
            }

            var item = args.SelectedItem as NavigationViewItem;
            var selectedView = item?.Tag.ToString();

            switch (selectedView)
            {
                case "Bounties":
                    ContentFrame.Navigate(typeof(BountiesParentView), null, args.RecommendedNavigationTransitionInfo);
                    break;

                case "Triumphs":
                    ContentFrame.Navigate(typeof(SettingsRootTriumphsView), ContentFrame, args.RecommendedNavigationTransitionInfo);
                    break;

                case "Insights":
                    ContentFrame.Navigate(typeof(SettingsInsightsView), ContentFrame, args.RecommendedNavigationTransitionInfo);
                    break;

                case "SeasonalChallenges":
                    ContentFrame.Navigate(typeof(WidgetSettingsTriumphsView), SeasonalChallengePresentationNode, args.RecommendedNavigationTransitionInfo);
                    break;
            }
        }

        private void NavView_OnBackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs navigationViewBackRequestedEventArgs)
        {
            if (ContentFrame.CanGoBack)
            {
                ContentFrame.GoBack();
            }
        }

        private async void CheckAuth()
        {
            if (AppState.Data.TokenData == null || !AppState.Data.TokenData.IsValid())
            {
                var widgetControl = new XboxGameBarWidgetControl(widget);
                await widgetControl.CloseAsync("WidgetMainSettings");
            }
        }

        private void CharacterSelectButtonClicked(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is long characterId)
            {
                Log.Info("CharacterSelectButtonClicked set active character");
                AppState.Data.ActiveCharacter = Characters.FirstOrDefault(v => v.CharacterId == characterId) ?? Characters.First();
                CharacterSelectFlyout.Hide();
            }
        }
    }
}
