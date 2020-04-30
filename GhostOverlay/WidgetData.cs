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
    public enum PropertyChanged
    {
        Profile,
        TrackedBounties,
        DefinitionsPath,
    }

    public class WidgetData
    {
        // number of requests to schedule profile updates. 
        public int ProfileScheduleRequesters = 0;
        public static int ProfileUpdateInterval = 15 * 1000;

        public bool DefinitionsLoaded => DefinitionsPath != null && DefinitionsPath.Length > 5;

        private readonly MyEventAggregator eventAggregator = new MyEventAggregator();

        private DestinyResponsesDestinyProfileResponse _profile;
        public DestinyResponsesDestinyProfileResponse Profile
        {
            get => _profile;

            set
            {
                if (value.Equals(_profile)) return;

                _profile = value;
                eventAggregator.Publish(PropertyChanged.Profile);
            }
        }

        private List<TrackedEntry> _trackedEntries = new List<TrackedEntry>();
        public List<TrackedEntry> TrackedEntries
        {
            get => _trackedEntries;
            set
            {
                if (value.Equals(_trackedEntries)) return;

                _trackedEntries = value;
                Debug.WriteLine("Setting new TrackedEntries");
                AppState.SaveTrackedEntries(_trackedEntries);
                eventAggregator.Publish(PropertyChanged.TrackedBounties);
            }
        }

        private string _definitionsPath;
        public string DefinitionsPath
        {
            get => _definitionsPath;
            set
            {
                if (value.Equals(_definitionsPath)) return;
                _definitionsPath = value;
                eventAggregator.Publish(PropertyChanged.DefinitionsPath);
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
            ProfileScheduleRequesters += 1;
            Debug.WriteLine($"ScheduleProfileUpdates, incrementing to {ProfileScheduleRequesters}");

            if (ProfileScheduleRequesters >= 2)
            {
                // Someone else has already started the schedule, so can just return
                Debug.WriteLine("Updates area already happening, so we can return");
                return;
            }

            while (ProfileScheduleRequesters > 0)
            {
                _ = UpdateProfile();
                await Task.Delay(ProfileUpdateInterval);
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
    }
}