﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BungieNetApi.Model;

namespace GhostOverlay
{
    public class WidgetData
    {
        public bool ProfileUpdateScheduled = false;
        public static int ProfileUpdateInterval = 10 * 1000;

        private readonly MyEventAggregator eventAggregator = new MyEventAggregator();

        private DestinyResponsesDestinyProfileResponse _profile;
        public DestinyResponsesDestinyProfileResponse Profile
        {
            get => _profile;

            set
            {
                if (value.Equals(_profile)) return;

                _profile = value;
                eventAggregator.Publish("Profile");
            }
        }

        private List<TrackedBounty> _trackedBounties = new List<TrackedBounty>();
        public List<TrackedBounty> TrackedBounties
        {
            get => _trackedBounties;
            set
            {
                if (value.Equals(_trackedBounties)) return;

                _trackedBounties = value;
                AppState.SaveTrackedBounties(_trackedBounties);
                eventAggregator.Publish("TrackedBounties");
            }
        }

        public async Task UpdateProfile()
        {
            if (Profile == null)
            {   
                Debug.WriteLine("Updating profile, for the first time using GetProfileForCurrentUser");
                Profile = await AppState.bungieApi.GetProfileForCurrentUser(AppState.bungieApi.DefaultProfileComponents);
            }
            else
            {
                Debug.WriteLine("Updating profile");
                Profile = await AppState.bungieApi.GetProfile(Profile.Profile.Data.UserInfo.MembershipType, Profile.Profile.Data.UserInfo.MembershipId, AppState.bungieApi.DefaultProfileComponents);
            }
        }

        public async void ScheduleProfileUpdates()
        {   
            if (ProfileUpdateScheduled)
            {
                return;
            }

            ProfileUpdateScheduled = true;

            while (ProfileUpdateScheduled)
            {
                await UpdateProfile();
                await Task.Delay(ProfileUpdateInterval);
            }
        }

        public bool ItemIsTracked(DestinyEntitiesItemsDestinyItemComponent item)
        {
            return item.ItemInstanceId == 0
                ? TrackedBounties.Any(vv => vv.ItemHash == item.ItemHash)
                : TrackedBounties.Any(vv => vv.ItemInstanceId == item.ItemInstanceId);
        }

        public void RestoreTrackedBountiesFromSettings()
        {
            TrackedBounties = AppState.RestoreTrackedBounties();
        }
    }
}