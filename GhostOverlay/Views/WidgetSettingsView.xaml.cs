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
        private XboxGameBarWidget widget;
        private readonly WidgetStateChangeNotifier eventAggregator = new WidgetStateChangeNotifier();
        public event PropertyChangedEventHandler PropertyChanged;
        private static readonly Logger Log = new Logger("WidgetSettingsView");

        private readonly ObservableCollection<Character> Characters = new ObservableCollection<Character>();

        private Character _activeCharacter;
        private Character ActiveCharacter
        {
            get => _activeCharacter;
            set
            {
                _activeCharacter = value;
                OnPropertyChanged();
            }
        }

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
            }
        }

        private async void UpdateCharacterList()
        {
            var charactersData = AppState.Data.Profile?.Characters.Data;
            if (charactersData == null) return;

            Characters.Clear();
            
            foreach (var destinyEntitiesCharactersDestinyCharacterComponent in charactersData.Values)
            {
                var newCharacter = new Character
                {
                    CharacterComponent = destinyEntitiesCharactersDestinyCharacterComponent,
                };

                await newCharacter.PopulatedExtendedDefinitions();

                Characters.Add(newCharacter);
            }

            if (AppState.Data.ActiveCharacter == null)
            {
                AppState.Data.ActiveCharacter = Characters.First();
            }
            else
            {
                AppState.Data.ActiveCharacter =
                    Characters.FirstOrDefault(v => v.CharacterId == AppState.Data.ActiveCharacter.CharacterId);
            }
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

        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
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
