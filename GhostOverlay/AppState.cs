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


    public enum SettingsKey
    {
        Language,
        DefinitionsPath,
    }

    public static class AppState
    {
        public static BungieApi bungieApi = new BungieApi();
        public static WidgetData WidgetData = new WidgetData();

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
    }
}