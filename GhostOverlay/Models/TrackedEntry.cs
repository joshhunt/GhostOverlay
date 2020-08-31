using System.ComponentModel;
using GhostOverlay.Models;
using GhostSharper.Models;
using Newtonsoft.Json;

namespace GhostOverlay
{
    public enum TrackedEntryType
    {
        Item = 0,
        Record = 1,
        DynamicTrackable = 2,
    }


    public class TrackedEntry : INotifyPropertyChanged
    {
        #pragma warning disable 67
        public event PropertyChangedEventHandler PropertyChanged;
        #pragma warning restore 67

        [JsonProperty("h")] public long Hash;
        [JsonProperty("d")] public DynamicTrackableType DynamicTrackableType;
        [JsonProperty("i")] public long InstanceId;
        [JsonProperty("o")] public long OwnerId;
        [JsonProperty("t")] public TrackedEntryType Type;
        [JsonProperty("s")] public bool ShowDescription;

        // TODO: does this work for DynamicTrackables
        public string UniqueKey => InstanceId == 0 ? $"{Hash}|${OwnerId}" : InstanceId.ToString();

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

        public static TrackedEntry FromInventoryItemComponent(DestinyItemComponent item, long characterId)
        {
            return new TrackedEntry
            {
                Type = TrackedEntryType.Item,
                Hash = item.ItemHash,
                InstanceId = item.ItemInstanceId,
                OwnerId = characterId
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

        public bool Matches(DynamicTrackableType dynamicTrackableType)
        {
            return Type == TrackedEntryType.DynamicTrackable && DynamicTrackableType == dynamicTrackableType;
        }

        public bool Matches(Triumph triumph)
        {
            return Type == TrackedEntryType.Record && Hash == triumph.Hash;
        }

        public override string ToString()
        {
            return $"TrackedEntry(Type: {Type}, Hash: {Hash}, InstanceId: {InstanceId}, OwnerId: {OwnerId}, DynamicTrackableType: {DynamicTrackableType})";
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
                ) &&
                (
                    DynamicTrackableType == input?.DynamicTrackableType ||
                    DynamicTrackableType.Equals(input?.DynamicTrackableType)
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
                hashCode = hashCode + 59 + DynamicTrackableType.GetHashCode();
                return hashCode;
            }
        }

        public static TrackedEntry FromDynamicTrackableType(DynamicTrackableType type)
        {
            return new TrackedEntry
            {
                Type = TrackedEntryType.DynamicTrackable,
                DynamicTrackableType = type
            };
        }
    }

}