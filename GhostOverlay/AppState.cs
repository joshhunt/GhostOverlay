using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using Windows.Storage;
using BungieNetApi.Model;
using Newtonsoft.Json;

namespace GhostOverlay
{
    public class TrackedBounty
    {
        [DataMember(Name = "h")] public long ItemHash;

        [DataMember(Name = "i")] public long ItemInstanceId;

        public override bool Equals(object obj)
        {
            var input = obj as TrackedBounty;

            return
                (
                    ItemInstanceId == input?.ItemInstanceId ||
                    ItemInstanceId.Equals(input?.ItemInstanceId)
                ) &&
                (
                    ItemHash == input?.ItemHash ||
                    ItemHash.Equals(input?.ItemHash)
                );
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = 41;
                hashCode = hashCode + 59 + ItemInstanceId.GetHashCode();
                hashCode = hashCode + 59 + ItemHash.GetHashCode();
                return hashCode;
            }
        }
    }

    // public struct SettingsKey
    // {
    //     public static string SelectedBounties = "SelectedBounties";
    //     public static string AccessToken = "AccessToken";
    //     public static string RefreshToken = "RefreshToken";
    //     public static string AccessTokenExpiration = "AccessTokenExpiration";
    //     public static string RefreshTokenExpiration = "RefreshTokenExpiration";
    //     public static string Language = "Language";
    //
    //     public static string SelectedBountiesItemHash = "ItemHash";
    //     public static string SelectedBountiesItemInstanceId = "ItemInstanceId";
    // }

    public enum SettingsKey
    {
        SelectedBounties,
        AccessToken,
        RefreshToken,
        AccessTokenExpiration,
        RefreshTokenExpiration,
        Language,
        DefinitionsPath,
    }

    public static class AppState
    {
        public static BungieApi bungieApi = new BungieApi();
        public static WidgetData WidgetData = new WidgetData();
        public static OAuthToken TokenData { get; set; }

        // TODO: I don't think we need to use this here any more?
        public static DestinyResponsesDestinyProfileResponse Profile { get; set; }

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
        }

        internal static void SaveTrackedBounties(List<TrackedBounty> trackedBounties)
        {
            var selectedBountiesSettings = new ApplicationDataCompositeValue();
            var counter = 0;

            foreach (var item in trackedBounties)
            {
                selectedBountiesSettings[counter.ToString()] = JsonConvert.SerializeObject(item);
                counter += 1;
            }

            Debug.WriteLine($"Actually saving setting {selectedBountiesSettings.Count} bounties");

            SaveSetting(SettingsKey.SelectedBounties, selectedBountiesSettings);
        }

        public static List<TrackedBounty> RestoreTrackedBounties()
        {
            Debug.WriteLine("Restoring saved bounties");
            var selectedBountiesSettings =
                ReadSetting(SettingsKey.SelectedBounties, new ApplicationDataCompositeValue());

            var trackedBounties = new List<TrackedBounty>();
            foreach (var settingsPair in selectedBountiesSettings)
            {
                var value = settingsPair.Value as string;
                var parsed = JsonConvert.DeserializeObject<TrackedBounty>(value);
                Debug.WriteLine(
                    $"Index {Convert.ToInt32(settingsPair.Key)}, got JSON from settings {value}. Parsed as ItemInstanceId: {parsed.ItemInstanceId}, ItemHash: {parsed.ItemHash}");
                trackedBounties.Insert(Convert.ToInt32(settingsPair.Key), parsed);
            }

            Debug.WriteLine($"Got {trackedBounties.Count} from settings");

            return trackedBounties;
        }

        public static void RestoreBungieTokenDataFromSettings()
        {
            TokenData = OAuthToken.RestoreTokenFromSettings();

            Debug.WriteLine("Restored TokenData:");
            Debug.WriteLine(TokenData.ToString());
        }
    }
}