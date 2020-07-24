using System.Collections.Generic;
using Windows.Storage;
using GhostOverlay.Models;
using Newtonsoft.Json;

namespace GhostOverlay
{
    public enum SettingsKey
    {
        AccessToken,
        RefreshToken,
        AccessTokenExpiration,
        RefreshTokenExpiration,
        AuthTokenVersion,
        Language,
        DefinitionsPath,
        TrackedEntries,
        AuthedBungieMembershipId,
        ShowDescriptions,
        ShowDevOptions,
    }

    public static class AppState
    {
        private static readonly Logger Log = new Logger("AppState");

        public static BungieApi bungieApi = new BungieApi();
        public static WidgetData Data = new WidgetData();
        public static readonly RemoteConfig RemoteConfigInstance = new RemoteConfig();
        public static RemoteConfigValues RemoteConfig => RemoteConfigInstance.Values;

        public static SettingsKey[] UserSpecificSettings = { SettingsKey.AccessToken, SettingsKey.RefreshToken, SettingsKey.AccessTokenExpiration, SettingsKey.RefreshTokenExpiration, SettingsKey.TrackedEntries, SettingsKey.AuthedBungieMembershipId, SettingsKey.Language };

        public static T ReadSetting<T>(SettingsKey key, T defaultValue)
        {
            var keyString = key.ToString();
            var localSettings = ApplicationData.Current.LocalSettings;

            if (localSettings.Values.ContainsKey(keyString)) return (T) localSettings.Values[keyString];

            if (null != defaultValue) return defaultValue;

            return default;
        }

        public static void SaveSetting<T>(SettingsKey key, T value)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[key.ToString()] = value;
            Log.Info("Saved {a} as {b}", key.ToString(), value);
        }

        public static void ClearSetting(SettingsKey key)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values.Remove(key.ToString());
        }

        internal static void SaveTrackedEntries(List<TrackedEntry> trackedEntries)
        {
            var json = JsonConvert.SerializeObject(trackedEntries);
            Log.Info("Saving {trackedEntriesCount} tracked entries", trackedEntries.Count);
            SaveSetting(SettingsKey.TrackedEntries, json);
        }

        public static List<TrackedEntry> GetTrackedEntriesFromSettings()
        {
            var json = ReadSetting(SettingsKey.TrackedEntries, "[]");
            Log.Info("Restored tracked entries json");
            Log.Debug("JSON: {json}", json);
            return JsonConvert.DeserializeObject<List<TrackedEntry>>(json);
        }

        public static void ClearUserSpecificSettings()
        {
            var localSettings = ApplicationData.Current.LocalSettings;

            foreach (var valuesKey in UserSpecificSettings)
            {
                localSettings.Values.Remove(valuesKey.ToString());
            }
        }
    }
}