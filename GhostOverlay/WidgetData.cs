using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Globalization;
using Windows.System.Threading;
using Windows.UI.Core;
using GhostOverlay.Models;
using GhostSharper.Models;
using DayOfWeek = System.DayOfWeek;

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
        ProfileError,
        DestinySettings,
        DefinitionsUpdating,
        Language,
        BustProfileRequests,
        ShowDescriptions,
        ShowDevOptions
    }

    public class WidgetValue<T>
    {
        private T backingValue;
        private readonly WidgetStateChangeNotifier eventAggregator = new WidgetStateChangeNotifier();

        public delegate void OnChange(T newValue);

        private readonly OnChange OnChangeFn;


        public WidgetValue(WidgetPropertyChanged k)
        {
            Key = k;
        }

        public WidgetValue(WidgetPropertyChanged k, T initialValue)
        {
            Key = k;
            backingValue = initialValue;
        }

        public WidgetValue(WidgetPropertyChanged k, T initialValue, OnChange onChange)
        {
            Key = k;
            backingValue = initialValue;
            OnChangeFn = onChange;
        }

        public WidgetPropertyChanged Key { get; set; }

        public T Value
        {
            get => backingValue;
            set
            {
                if (EqualityComparer<T>.Default.Equals(backingValue, value)) return;

                backingValue = value;
                OnChangeFn?.Invoke(backingValue);

                eventAggregator.Publish(Key);
            }
        }
    }

    public class WidgetData
    {
        private readonly Logger Log = new Logger("WidgetData");

        public static double ActiveProfileUpdateSeconds = 30;
        public static double InactiveProfileUpdateSeconds = 60;

        // number of requests to schedule profile updates. 
        public int ProfileScheduleRequesters = 0;
        public DateTime ProfileUpdatedTime = DateTime.MinValue;
        public DateTime LastProfileBustTime = DateTime.MinValue;
        public bool WidgetsAreVisible { get; set; }
        public int NumberOfSameProfileUpdates = 0;

        public TimeSpan UpdateProfileInterval;
        public ThreadPoolTimer UpdateProfileTimer;

        public bool DefinitionsLoaded => DefinitionsPath != null && DefinitionsPath.Length > 5;

        private readonly WidgetStateChangeNotifier eventAggregator = new WidgetStateChangeNotifier();

        public WidgetValue<CoreSettingsConfiguration> DestinySettings = new WidgetValue<CoreSettingsConfiguration>(WidgetPropertyChanged.DestinySettings);
        public WidgetValue<bool> DefinitionsUpdating = new WidgetValue<bool>(WidgetPropertyChanged.DefinitionsUpdating, false);
        public WidgetValue<bool> BustProfileRequests = new WidgetValue<bool>(WidgetPropertyChanged.BustProfileRequests, false);
        public WidgetValue<string> ProfileError = new WidgetValue<string>(WidgetPropertyChanged.ProfileError);

        public bool CrucibleMapIsTracked => TrackedEntries.Any(v => v.Type == TrackedEntryType.DynamicTrackable && v.DynamicTrackableType == DynamicTrackableType.CrucibleMap);

        public WidgetValue<string> Language = new WidgetValue<string>(WidgetPropertyChanged.Language, "",
            (newValue) =>
            {
                AppState.SaveSetting(SettingsKey.Language, newValue);
            });

        public WidgetValue<bool> ShowDescriptions = new WidgetValue<bool>(WidgetPropertyChanged.ShowDescriptions, true,
            (newValue) =>
            {
                AppState.SaveSetting(SettingsKey.ShowDescriptions, newValue);
            });

        public WidgetValue<bool> ShowDevOptions = new WidgetValue<bool>(WidgetPropertyChanged.ShowDevOptions, true,
            (newValue) =>
            {
                AppState.SaveSetting(SettingsKey.ShowDevOptions, newValue);
            });

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

        private DestinyProfileResponse _profile;
        public DestinyProfileResponse Profile
        {
            get => _profile;

            set
            {   
                if (value?.Equals(_profile) ?? false)
                {
                    NumberOfSameProfileUpdates += 1;
                    Log.Info("New profile is the same, so skipping");
                    return;
                }

                Log.Info("New profile is very different!!!");
                NumberOfSameProfileUpdates = 0;
                _profile = value;
                ProfileUpdatedTime = DateTime.Now;
                ProfileError.Value = null;
                eventAggregator.Publish(WidgetPropertyChanged.Profile);
            }
        }

        private TrackableOwner _activeCharacter;
        public TrackableOwner ActiveCharacter
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
            Log.Info("----- UpdateProfile");
            ProfileIsUpdating = true;

            try
            {
                if (Profile == null)
                {
                    Log.Info("Updating profile, for the first time using GetProfileForCurrentUser");
                    Profile = await AppState.Api.GetProfileForCurrentUser(AppState.Api.DefaultProfileComponents);
                }
                else
                {
                    var shouldBustProfile = (BustProfileRequests.Value ||
                                             AppState.RemoteConfig.AutoProfileBust ||
                                             (AppState.RemoteConfig.CrucibleMapTrackerAutoProfileBust &&
                                              CrucibleMapIsTracked)) && NumberOfSameProfileUpdates < 3;
                    Log.Info("shouldBustProfile: {shouldBustProfile}, NumberOfSameProfileUpdates: {NumberOfSameProfileUpdates}", shouldBustProfile, NumberOfSameProfileUpdates);

                    if (shouldBustProfile)
                    {
                        await AppState.Api.CacheBust(Profile);
                    }

                    Profile = await AppState.Api.GetProfile(Profile.Profile.Data.UserInfo.MembershipType, Profile.Profile.Data.UserInfo.MembershipId, AppState.Api.DefaultProfileComponents);
                    ProfileError.Value = "";
                }
            }
            catch (Exception err)
            {
                HandleProfileError(err);
            }
            
            ProfileIsUpdating = false;
        }

        private void HandleProfileError(Exception err)
        {
            Log.Error("Error with UpdateProfile {err}", err);

            if (err is BungieApiException bungieError)
            {
                if (bungieError.Response != null)
                {
                    ProfileError.Value = $"Bungie API Error: {bungieError.Response.Message}";
                    return;
                }

                ProfileError.Value = $"Unknown API Error: {bungieError.Message}";
                return;
            }

            ProfileError.Value = $"Unknown Error: {err.Message}";
        }

        public async void ScheduleProfileUpdates()
        {   
            ProfileScheduleRequesters += 1;
            Log.Info("ScheduleProfileUpdates, incrementing to {ProfileScheduleRequesters}", ProfileScheduleRequesters);

            if (ProfileScheduleRequesters >= 2)
            {
                // Someone else has already started the schedule, so can just return
                Log.Info("Updates already happening, so we can return");
                await UpdateProfile();
                return;
            }

            Log.Info("Running initial Update Profile");
            await UpdateProfile();

            UpdateProfileInterval = TimeSpan.FromSeconds(ActiveProfileUpdateSeconds);
            UpdateProfileTimer = ThreadPoolTimer.CreatePeriodicTimer(OnProfileUpdateTimerInvocation, UpdateProfileInterval, OnProfileUpdateTimerCancellation);
        }

        public async void OnProfileUpdateTimerInvocation(ThreadPoolTimer timer)
        {
            await UpdateProfile();

            var delay = (WidgetsAreVisible && NumberOfSameProfileUpdates < 5) ? ActiveProfileUpdateSeconds : InactiveProfileUpdateSeconds;
            Log.Info("Waiting {delaySeconds}s before fetching profile again", delay);

            if (Math.Abs(delay - timer.Period.TotalSeconds) > 5)
            {
                Log.Info("Timer period {period}s does not match the calculated delay {delay}s. Cancelling periodic timer, and a new one should be made", timer.Period.TotalMilliseconds, delay);
                UpdateProfileInterval = TimeSpan.FromSeconds(delay);
                timer.Cancel();
            }
        }

        public void OnProfileUpdateTimerCancellation(ThreadPoolTimer timer)
        {
            if (ProfileScheduleRequesters > 0)
            {
                Log.Info("Creating new periodic timer in cancellation fn for {period}s", UpdateProfileInterval.TotalMilliseconds);
                UpdateProfileTimer = ThreadPoolTimer.CreatePeriodicTimer(OnProfileUpdateTimerInvocation, UpdateProfileInterval, OnProfileUpdateTimerCancellation);
            }
        }

        public void CancelAllScheduledProfileUpdates()
        {
            ProfileScheduleRequesters = 0;
            UpdateProfileTimer.Cancel();
        }

        public void WidgetVisibilityChanged()
        {
            var sinceLastUpdate = DateTime.Now - ProfileUpdatedTime;
            if (WidgetsAreVisible && !ProfileIsUpdating &&
                (sinceLastUpdate.TotalSeconds > ActiveProfileUpdateSeconds) && (ProfileScheduleRequesters > 0))
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
            UpdateProfileTimer?.Cancel();
        }

        public bool IsTracked(Item item)
        {
            return TrackedEntries.Any(v => v.Matches(item));
        }

        public bool IsTracked(Triumph triumph)
        {
            return TrackedEntries.Any(v => v.Matches(triumph));
        }

        public void SignOutAndResetAllData()
        {
            CancelAllScheduledProfileUpdates();

            Profile = default;
            TrackedEntries = new List<TrackedEntry>();
            TokenData = new OAuthToken();

            AppState.ClearUserSpecificSettings();
        }

        public void RestoreSettings()
        {
            Log.Info("Restoring settings");
            TokenData = OAuthToken.RestoreTokenFromSettings();

            TrackedEntries = AppState.GetTrackedEntriesFromSettings();
            TrackedEntries.RemoveAll(v => v.Hash == 0 && v.InstanceId == 0);

            Language.Value = AppState.ReadSetting(SettingsKey.Language, "@@UNSET"); // Default value is set in Definitions

            ShowDescriptions.Value = AppState.ReadSetting(SettingsKey.ShowDescriptions, true);
            ShowDevOptions.Value = AppState.ReadSetting(SettingsKey.ShowDevOptions, false);
        }

        public async Task<CoreSettingsConfiguration> UpdateDestinySettings()
        {
            DestinySettings.Value = await AppState.Api.GetSettings();

            return DestinySettings.Value;
        }

        public async Task UpdateDefinitionsLanguage(string newLanguage)
        {
            if (newLanguage == Language.Value) return;

            Language.Value = newLanguage;
            DefinitionsUpdating.Value = true;
            await Definitions.CheckForLatestDefinitions();
            DefinitionsUpdating.Value = false;
        }

        public async Task ForceProfileUpdate()
        {
            ProfileIsUpdating = true;

            try
            {
                await AppState.Api.CacheBust(Profile);
                Profile = await AppState.Api.GetProfile(Profile.Profile.Data.UserInfo.MembershipType, Profile.Profile.Data.UserInfo.MembershipId, AppState.Api.DefaultProfileComponents);
            }
            catch (Exception err)
            {
                HandleProfileError(err);
            }

            NumberOfSameProfileUpdates = 0;
            ProfileIsUpdating = false;
        }
    }
}