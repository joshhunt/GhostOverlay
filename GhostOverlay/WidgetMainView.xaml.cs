using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel.Channels;
using System.Threading;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Microsoft.Gaming.XboxGameBar;
using SQLitePCL;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace GhostOverlay
{
    public sealed partial class WidgetMainView : Page, ISubscriber<PropertyChanged>
    {
        public List<Bounty> AllBounties = new List<Bounty>();
        public ObservableCollection<Bounty> TrackedBounties = new ObservableCollection<Bounty>();
        private XboxGameBarWidget widget;

        private enum VisualState
        {
            None,
            Empty,
            InitialProfileLoad,
            ProfileError,
            DefinitionsLoading,
        }

        public WidgetMainView()
        {
            InitializeComponent();
            var eventAggregator = new MyEventAggregator();
            eventAggregator.Subscribe(this);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            widget = e.Parameter as XboxGameBarWidget;

            if (widget == null)
            {
                Debug.WriteLine("Widget parameter is null");
                return;
            }

            SetVisualState(VisualState.InitialProfileLoad);

            Debug.WriteLine("WidgetMainView OnNavigatedTo setting widget settings");
            widget.MaxWindowSize = new Size(1500, 1500);
            widget.MinWindowSize = new Size(200, 100);
            widget.HorizontalResizeSupported = true;
            widget.VerticalResizeSupported = true;
            widget.SettingsSupported = true;
            widget.SettingsClicked += Widget_SettingsClicked;

            Debug.WriteLine("WidgetMainView OnNavigatedTo scheduling stuff");
            AppState.WidgetData.ScheduleProfileUpdates();

            UpdateFromProfile();
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            AppState.WidgetData.ProfileUpdateScheduled = false;
        }

        public void HandleMessage(PropertyChanged message)
        {
            Debug.WriteLine($"WidgetMainView: property {message} changed!");

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
            Debug.WriteLine("UpdateFromProfile");

            var profile = AppState.WidgetData.Profile;

            if (profile?.CharacterInventories?.Data == null)
            {
                if (profile?.Profile?.Data?.UserInfo?.MembershipId != null)
                {
                    Debug.WriteLine("no inventory data in profile, returning");
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

            AllBounties = Bounty.BountiesFromProfile(profile, addCompletedBounties: false);
            UpdateTrackedBounties();
        }

        private void UpdateTrackedBounties()
        {
            Debug.WriteLine("UpdateTrackedBounties");

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
    }
}