﻿using System;
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
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Microsoft.Gaming.XboxGameBar;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace GhostOverlay
{
    public sealed partial class WidgetMainView : Page, ISubscriber<string>
    {
        public List<Bounty> AllBounties = new List<Bounty>();
        public ObservableCollection<Bounty> TrackedBounties = new ObservableCollection<Bounty>();
        private XboxGameBarWidget widget;

        public WidgetMainView()
        {
            InitializeComponent();
            var eventAggregator = new MyEventAggregator();
            eventAggregator.Subscribe(this);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Debug.WriteLine("WidgetMainView OnNavigatedTo");

            widget = e.Parameter as XboxGameBarWidget;

            if (widget == null)
            {
                Debug.WriteLine("Widget parameter is null");
                return;
            }

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

        public void HandleMessage(string message)
        {
            Debug.WriteLine($"property {message} changed!");

            switch (message)
            {
                case "Profile":
                    UpdateFromProfile();
                    break;

                case "TrackedBounties":
                    UpdateTrackedBounties();
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
                    MessageText.Visibility = Visibility.Visible;
                    MessageText.Text = "Inventory data is missing in profile. This is an error. Maybe auth has failed or expired?";
                    Debug.WriteLine("no inventory data in profile, returning");
                }
                
                return;
            }

            LoadingRing.IsActive = false;
            LoadingRing.Visibility = Visibility.Collapsed;
            MessageText.Visibility = Visibility.Collapsed;

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

            if (TrackedBounties.Count == 0)
            {
                MessageText.Visibility = Visibility.Visible;
                MessageText.Text = "Track bounties from settings to see them here";
            }
            else
            {
                MessageText.Visibility = Visibility.Collapsed;
            }

            TrackedBountiesCollection.Source = groupedBounties;
        }

        private async void Widget_SettingsClicked(XboxGameBarWidget sender, object args)
        {
            await widget.ActivateSettingsAsync();
        }
    }
}