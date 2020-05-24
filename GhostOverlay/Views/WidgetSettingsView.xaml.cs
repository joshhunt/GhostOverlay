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
        private readonly MyEventAggregator eventAggregator = new MyEventAggregator();
        public event PropertyChangedEventHandler PropertyChanged;

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
            widget = e.Parameter as XboxGameBarWidget;

            if (widget != null)
            {
                widget.MaxWindowSize = new Size(1940, 2000);
                widget.MinWindowSize = new Size(200, 100);
                widget.HorizontalResizeSupported = true;
                widget.VerticalResizeSupported = true;
                widget.SettingsSupported = false;
            }

            NavView.SelectedItem = NavView.MenuItems[0];

            eventAggregator.Subscribe(this);
            AppState.Data.ScheduleProfileUpdates();
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            AppState.Data.UnscheduleProfileUpdates();
        }

        public void HandleMessage(WidgetPropertyChanged message)
        {
            //Debug.WriteLine($"[WidgetSettingsView] HandleMessage {message}");
            switch (message)
            {
                case WidgetPropertyChanged.TokenData:
                    CheckAuth();
                    break;

                case WidgetPropertyChanged.Profile:
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
        }

        private void UpdateActiveCharacter()
        {
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
                    ContentFrame.Navigate(typeof(WidgetSettingsBountiesView), null, args.RecommendedNavigationTransitionInfo);
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

        private void Log(string message)
        {
            Debug.WriteLine($"[WidgetSettingsView] {message}");
        }

        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void CharacterSelectButtonClicked(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is long characterId)
            {
                AppState.Data.ActiveCharacter = Characters.FirstOrDefault(v => v.CharacterId == characterId) ?? Characters.First();
                CharacterSelectFlyout.Hide();
            }
        }
    }
}
