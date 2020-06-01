using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BungieNetApi.Model;
using GhostOverlay.Models;
using Microsoft.AppCenter.Crashes;

namespace GhostOverlay
{
    public enum WidgetPropertyChanged
    {
        Profile,
        ProfileUpdating,
        TrackedItems,
        DefinitionsPath,
        ActiveCharacter,
        TokenData,
        ProfileError
    }

    public class WidgetValue<T>
    {
        private T backingValue;
        private readonly WidgetStateChangeNotifier eventAggregator = new WidgetStateChangeNotifier();

        public WidgetValue(WidgetPropertyChanged k)
        {
            Key = k;
        }

        public WidgetValue(WidgetPropertyChanged k, T initialValue)
        {
            Key = k;
            backingValue = initialValue;
        }

        public WidgetPropertyChanged Key { get; set; }

        public T Value
        {
            get => backingValue;
            set
            {
                if (EqualityComparer<T>.Default.Equals(backingValue, value)) return;

                backingValue = value;
                eventAggregator.Publish(Key);
            }
        }
    }

    public class WidgetData
    {
        private readonly Logger Log = new Logger("WidgetData");

        // number of requests to schedule profile updates. 
        public int ProfileScheduleRequesters = 0;
        public static int ActiveProfileUpdateInterval = 15 * 1000;
        public static int InactiveProfileUpdateInterval = 60 * 1000;
        public DateTime ProfileUpdatedTime = DateTime.MinValue;
        public bool WidgetsAreVisible { get; set; }

        public bool DefinitionsLoaded => DefinitionsPath != null && DefinitionsPath.Length > 5;

        private readonly WidgetStateChangeNotifier eventAggregator = new WidgetStateChangeNotifier();

        public WidgetValue<string> ProfileError = new WidgetValue<string>(WidgetPropertyChanged.ProfileError, "");

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
                    Log.Info("New profile is the same, so skipping");
                    return;
                }

                Log.Info("New profile is very different!!!");
                _profile = value;
                ProfileUpdatedTime = DateTime.Now;
                eventAggregator.Publish(WidgetPropertyChanged.Profile);
            }
        }

        private Character _activeCharacter;
        public Character ActiveCharacter
        {
            get => _activeCharacter;

            set
            {
                if (value?.Equals(_activeCharacter) ?? false) return;

                _activeCharacter = value;
                eventAggregator.Publish(WidgetPropertyChanged.ActiveCharacter);
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
                    Log.Info("Updating profile, for the first time using GetProfileForCurrentUser");
                    Profile = await AppState.bungieApi.GetProfileForCurrentUser(AppState.bungieApi.DefaultProfileComponents);
                }
                else
                {
                    Profile = await AppState.bungieApi.GetProfile(Profile.Profile.Data.UserInfo.MembershipType, Profile.Profile.Data.UserInfo.MembershipId, AppState.bungieApi.DefaultProfileComponents);
                    ProfileError.Value = "";
                }
            }
            catch (Exception err)
            {
                Log.Error("Error with UpdateProfile", err);
                Crashes.TrackError(err);
                ProfileError.Value = err.Message;
            }
            
            ProfileIsUpdating = false;
        }

        public async void ScheduleProfileUpdates()
        {   
            ProfileScheduleRequesters += 1;
            Log.Info("ScheduleProfileUpdates, incrementing to {ProfileScheduleRequesters}", ProfileScheduleRequesters);

            if (ProfileScheduleRequesters >= 2)
            {
                // Someone else has already started the schedule, so can just return
                Log.Info("Updates area already happening, so we can return");
                await UpdateProfile();
                return;
            }

            while (ProfileScheduleRequesters > 0)
            {
                await UpdateProfile();

                var delay = WidgetsAreVisible ? ActiveProfileUpdateInterval : InactiveProfileUpdateInterval;
                Log.Info("Waiting {delaySeconds}s before fetching profile again", delay / 1000);
                
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
            if (WidgetsAreVisible && !ProfileIsUpdating && sinceLastUpdate.TotalMilliseconds > ActiveProfileUpdateInterval && ProfileScheduleRequesters > 0)
            {
                Log.Info("Visiblity changed, updating profile");
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
        }

        public void RestoreBungieTokenDataFromSettings()
        {
            TokenData = OAuthToken.RestoreTokenFromSettings();
            Log.Info("Restored TokenData");
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