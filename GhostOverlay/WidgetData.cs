using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BungieNetApi.Model;
using GhostOverlay.Models;

namespace GhostOverlay
{
    public enum WidgetPropertyChanged
    {
        Profile,
        ProfileUpdating,
        TrackedItems,
        DefinitionsPath,
        TokenData
    }

    public class WidgetData
    {
        // number of requests to schedule profile updates. 
        public int ProfileScheduleRequesters = 0;
        public static int ActiveProfileUpdateInterval = 15 * 1000;
        public static int InactiveProfileUpdateInterval = 60 * 1000;
        public DateTime ProfileUpdatedTime = DateTime.MinValue;
        public bool WidgetsAreVisible { get; set; }

        public bool DefinitionsLoaded => DefinitionsPath != null && DefinitionsPath.Length > 5;

        private readonly MyEventAggregator eventAggregator = new MyEventAggregator();

        private bool _profileIsUpdating;
        public bool ProfileIsUpdating
        {
            get => _profileIsUpdating;
            set
            {
                _profileIsUpdating = value;
                eventAggregator.Publish(WidgetPropertyChanged.ProfileUpdating);
            }
        }

        private DestinyResponsesDestinyProfileResponse _profile;
        public DestinyResponsesDestinyProfileResponse Profile
        {
            get => _profile;

            set
            {
                if (value?.Equals(_profile) ?? false)
                {
                    Debug.WriteLine("New profile is the same, so skipping");
                    return;
                }

                Debug.WriteLine("New profile is very different!!!");
                _profile = value;
                ProfileUpdatedTime = DateTime.Now;
                eventAggregator.Publish(WidgetPropertyChanged.Profile);
            }
        }

        private List<TrackedEntry> _trackedEntries = new List<TrackedEntry>();
        public List<TrackedEntry> TrackedEntries
        {
            get => _trackedEntries;
            set
            {
                if (value?.Equals(_trackedEntries) ?? false) return;

                _trackedEntries = value;
                AppState.SaveTrackedEntries(_trackedEntries);
                eventAggregator.Publish(WidgetPropertyChanged.TrackedItems);
            }
        }

        private string _definitionsPath;
        public string DefinitionsPath
        {
            get => _definitionsPath;
            set
            {
                if (value?.Equals(_definitionsPath) ?? false) return;

                _definitionsPath = value;
                eventAggregator.Publish(WidgetPropertyChanged.DefinitionsPath);
            }
        }

        private OAuthToken _tokenData;
        public OAuthToken TokenData
        {
            get => _tokenData;
            set
            {
                _tokenData = value;
                eventAggregator.Publish(WidgetPropertyChanged.TokenData);
            }
        }

        public async Task UpdateProfile()
        {
            ProfileIsUpdating = true;

            try
            {
                if (Profile == null)
                {
                    Debug.WriteLine("Updating profile, for the first time using GetProfileForCurrentUser");
                    Profile = await AppState.bungieApi.GetProfileForCurrentUser(AppState.bungieApi.DefaultProfileComponents);
                }
                else
                {
                    Profile = await AppState.bungieApi.GetProfile(Profile.Profile.Data.UserInfo.MembershipType, Profile.Profile.Data.UserInfo.MembershipId, AppState.bungieApi.DefaultProfileComponents);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error trying to UpdateProfile:");
                Debug.WriteLine(e);
            }
            
            ProfileIsUpdating = false;
        }

        public async void ScheduleProfileUpdates()
        {   
            ProfileScheduleRequesters += 1;
            Debug.WriteLine($"ScheduleProfileUpdates, incrementing to {ProfileScheduleRequesters}");

            if (ProfileScheduleRequesters >= 2)
            {
                // Someone else has already started the schedule, so can just return
                Debug.WriteLine("Updates area already happening, so we can return");
                await UpdateProfile();
                return;
            }

            while (ProfileScheduleRequesters > 0)
            {
                await UpdateProfile();

                var delay = WidgetsAreVisible ? ActiveProfileUpdateInterval : InactiveProfileUpdateInterval;
                Debug.WriteLine($"Waiting {delay / 1000}s before fetching profile again");
                
                await Task.Delay(delay);
            }
        }

        public void CancelAllScheduledProfileUpdates()
        {
            ProfileScheduleRequesters = 0;
        }

        public void WidgetVisibilityChanged()
        {
            var sinceLastUpdate = DateTime.Now - ProfileUpdatedTime;
            if (WidgetsAreVisible && !ProfileIsUpdating && sinceLastUpdate.TotalMilliseconds > ActiveProfileUpdateInterval)
            {   
                Debug.WriteLine("Visiblity changed, updating profile");
                _ = UpdateProfile();
            }
        }

        public void UnscheduleProfileUpdates()
        {
            if (ProfileScheduleRequesters < 1)
            {
                return;
            }

            ProfileScheduleRequesters -= 1;
        }

        public bool IsTracked(Item item)
        {
            return TrackedEntries.Any(v => v.Matches(item));
        }

        public bool IsTracked(Triumph triumph)
        {
            return TrackedEntries.Any(v => v.Matches(triumph));
        }

        public void RestoreTrackedBountiesFromSettings()
        {
            TrackedEntries = AppState.GetTrackedEntriesFromSettings();
            TrackedEntries.RemoveAll(v => v.Hash == 0 && v.InstanceId == 0);

            foreach (var trackedEntry in TrackedEntries)
                Debug.WriteLine($"  Restored {trackedEntry}");
        }

        public void RestoreBungieTokenDataFromSettings()
        {
            TokenData = OAuthToken.RestoreTokenFromSettings();

            Debug.WriteLine("Restored TokenData:");
            Debug.WriteLine(TokenData.ToString());
        }

        public void SignOutAndResetAllData()
        {
            CancelAllScheduledProfileUpdates();

            Profile = default;
            TrackedEntries = new List<TrackedEntry>();
            TokenData = new OAuthToken();

            AppState.ClearUserSpecificSettings();
        }
    }
}