using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Gaming.XboxGameBar;
using Microsoft.Toolkit.Uwp.UI.Extensions;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace GhostOverlay
{
    public sealed partial class WidgetMainView : Page, ISubscriber<PropertyChanged>
    {
        private enum VisualState
        {
            None,
            Empty,
            InitialProfileLoad,
            ProfileError,
            DefinitionsLoading
        }

        public TimerViewModel updateTimer;
        public List<Bounty> AllBounties = new List<Bounty>();
        public ObservableCollection<Bounty> TrackedBounties = new ObservableCollection<Bounty>();
        private XboxGameBarWidget widget;
        private readonly MyEventAggregator eventAggregator = new MyEventAggregator();

        public WidgetMainView()
        {
            InitializeComponent();
            eventAggregator.Subscribe(this);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            widget = e.Parameter as XboxGameBarWidget;
            updateTimer = new TimerViewModel();

            if (widget != null)
            {
                widget.MaxWindowSize = new Size(1500, 1500);
                widget.MinWindowSize = new Size(200, 100);
                widget.HorizontalResizeSupported = true;
                widget.VerticalResizeSupported = true;
                widget.SettingsSupported = true;
                widget.SettingsClicked += Widget_SettingsClicked;
                widget.RequestedThemeChanged += Widget_RequestedThemeChanged;
                Widget_RequestedThemeChanged(widget, null);
            }

            SetVisualState(VisualState.InitialProfileLoad);

            AppState.WidgetData.ScheduleProfileUpdates();
            UpdateFromProfile();
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            eventAggregator.Subscribe(this);
            AppState.WidgetData.UnscheduleProfileUpdates();
        }

        public void HandleMessage(PropertyChanged message)
        {
            switch (message)
            {
                case PropertyChanged.Profile:
                case PropertyChanged.DefinitionsPath:
                    UpdateFromProfile();
                    break;

                case PropertyChanged.TrackedBounties:
                    UpdateTrackedBounties();
                    break;
            }
        }

        private void SetVisualState(VisualState state)
        {
            EmptyState.Visibility = Visibility.Collapsed;
            InitialProfileLoadState.Visibility = Visibility.Collapsed;
            ProfileErrorState.Visibility = Visibility.Collapsed;
            DefinitionsLoadingState.Visibility = Visibility.Collapsed;

            switch (state)
            {
                case VisualState.None:
                    break;

                case VisualState.Empty:
                    EmptyState.Visibility = Visibility.Visible;
                    break;

                case VisualState.InitialProfileLoad:
                    InitialProfileLoadState.Visibility = Visibility.Visible;
                    break;

                case VisualState.ProfileError:
                    ProfileErrorState.Visibility = Visibility.Visible;
                    break;

                case VisualState.DefinitionsLoading:
                    DefinitionsLoadingState.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void UpdateFromProfile()
        {
            var profile = AppState.WidgetData.Profile;

            if (profile?.CharacterInventories?.Data == null)
            {
                if (profile?.Profile?.Data?.UserInfo?.MembershipId != null)
                {
                    SetVisualState(VisualState.ProfileError);
                }

                return;
            }

            if (!AppState.WidgetData.DefinitionsLoaded)
            {
                SetVisualState(VisualState.DefinitionsLoading);
                return;
            }

            SetVisualState(VisualState.None);

            AllBounties = Bounty.BountiesFromProfile(profile, true);
            UpdateTrackedBounties();
        }

        private void UpdateTrackedBounties()
        {
            // TODO: Rather than completely clearing, only add/remove where needed
            TrackedBounties.Clear();
            foreach (var bounty in AllBounties)
            {
                if (!AppState.WidgetData.ItemIsTracked(bounty.Item)) continue;
                TrackedBounties.Add(bounty);
            }

            var groupedBounties =
                from t in TrackedBounties
                group t by t.OwnerCharacter
                into g
                select g;

            SetVisualState(TrackedBounties.Count == 0 ? VisualState.Empty : VisualState.None);

            TrackedBountiesCollection.Source = groupedBounties;
        }

        private async void Widget_SettingsClicked(XboxGameBarWidget sender, object args)
        {
            await widget.ActivateSettingsAsync();
        }

        private async void SettingsButton_OnClick(object sender, RoutedEventArgs e)
        {
            await widget.ActivateSettingsAsync();
        }

        private async void Widget_RequestedThemeChanged(XboxGameBarWidget sender, object args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () =>
                {
                    Background = widget.RequestedTheme == ElementTheme.Dark
                        ? new SolidColorBrush(Color.FromArgb(255, 0, 0, 0))
                        : new SolidColorBrush(Color.FromArgb(255, 76, 76, 76));
                });
        }
    }
}