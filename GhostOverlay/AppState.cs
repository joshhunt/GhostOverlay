using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using Windows.Storage;
using BungieNetApi.Model;
using GhostOverlay.Models;
using Newtonsoft.Json;

namespace GhostOverlay
{
    public enum TrackedEntryType
    {
        Item = 0,
        Record = 1,
    }

    public class TrackedEntry
    {
        [JsonProperty("h")] public long Hash;
        [JsonProperty("i")] public long InstanceId;
        [JsonProperty("o")] public long OwnerId;
        [JsonProperty("t")] public TrackedEntryType Type;

        public static TrackedEntry FromItem(Item item)
        {
            return new TrackedEntry
            {
                Type = TrackedEntryType.Item,
                Hash = item.ItemHash,
                InstanceId = item.ItemInstanceId,
                OwnerId = item.OwnerCharacter.CharacterComponent.CharacterId
            };
        }

        public static TrackedEntry FromTriumph(Triumph triumph)
        {
            return new TrackedEntry
            {
                Type = TrackedEntryType.Record,
                Hash = triumph.Hash
            };
        }

        public bool Matches(Item item)
        {
            return Type == TrackedEntryType.Item && Hash == item.ItemHash &&
                   InstanceId == item.ItemInstanceId &&
                   OwnerId == item.OwnerCharacter.CharacterComponent.CharacterId;
        }

        public bool Matches(Triumph triumph)
        {
            return Type == TrackedEntryType.Record && Hash == triumph.Hash;
        }

        public override string ToString()
        {
            return $"TrackedEntry(Type: {Type}, Hash: {Hash}, InstanceId: {InstanceId}, OwnerId: {OwnerId})";
        }

        public override bool Equals(object obj)
        {
            var input = obj as TrackedEntry;

            return
                (
                    Type == input?.Type ||
                    Type.Equals(input?.Type)
                ) &&
                (
                    Hash == input?.Hash ||
                    Hash.Equals(input?.Hash)
                ) &&
                (
                    InstanceId == input?.InstanceId ||
                    InstanceId.Equals(input?.InstanceId)
                ) &&
                (
                    OwnerId == input?.OwnerId ||
                    OwnerId.Equals(input?.OwnerId)
                );
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = 41;
                hashCode = hashCode + 59 + Type.GetHashCode();
                hashCode = hashCode + 59 + Hash.GetHashCode();
                hashCode = hashCode + 59 + InstanceId.GetHashCode();
                hashCode = hashCode + 59 + OwnerId.GetHashCode();
                return hashCode;
            }
        }
    }

    public enum SettingsKey
    {
        AccessToken,
        RefreshToken,
        AccessTokenExpiration,
        RefreshTokenExpiration,
        AuthTokenVersion,
        Language,
        DefinitionsPath,
        TrackedEntries
    }

    public static class AppState
    {
        public static BungieApi bungieApi = new BungieApi();
        public static WidgetData Data = new WidgetData();

        public static SettingsKey[] UserSpecificSettings = { SettingsKey.AccessToken, SettingsKey.RefreshToken, SettingsKey.AccessTokenExpiration, SettingsKey.RefreshTokenExpiration, SettingsKey.TrackedEntries };

        [Obsolete("Use AppState.Widgetdata.TokenData instead.")]
        public static OAuthToken TokenData { get; set; }

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

        public static void ClearSetting(SettingsKey key)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values.Remove(key.ToString());
        }

        internal static void SaveTrackedEntries(List<TrackedEntry> trackedEntries)
        {
            var json = JsonConvert.SerializeObject(trackedEntries);
            Debug.WriteLine($"Actually saving setting {trackedEntries.Count} entries");
            SaveSetting(SettingsKey.TrackedEntries, json);
        }

        public static List<TrackedEntry> GetTrackedEntriesFromSettings()
        {
            var json = ReadSetting(SettingsKey.TrackedEntries, "[]");
            Debug.WriteLine($"Restored {json}");
            return JsonConvert.DeserializeObject<List<TrackedEntry>>(json);
        }

        [Obsolete("Use AppState.Widgetdata.RestoreBungieTokenDataFromSettings instead.")]
        public static void RestoreBungieTokenDataFromSettings() {}

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