using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using GhostSharper.Models;

namespace GhostOverlay.Models
{
    public class Item : ITrackable
    {
        #pragma warning disable 67
        public event PropertyChangedEventHandler PropertyChanged;
        #pragma warning restore 67

        public static uint ArmourCategoryHash = 20;
        public static long PersuitsBucketHash = 1345459588;

        public static string QuestTraitId = "inventory_filtering.quest";
        public static string BountyTraitId = "inventory_filtering.bounty";

        public long ItemHash = 0;
        public long ItemInstanceId = 0;
        public long BucketHash { get; set; }

        public TrackedEntry TrackedEntry { get; set; }

        public DestinyInventoryItemDefinition Definition { get; set; }
        public DestinyDisplayPropertiesDefinition DisplayProperties => Definition.DisplayProperties;
        public List<Objective> Objectives { get; set; }
        public TrackableOwner Owner { get; set; }

        public bool IsCompleted => Objectives?.TrueForAll(v => v.Progress.Complete) ?? false;

        public bool ShowDescription =>
            !IsCompleted && ((TrackedEntry?.ShowDescription ?? true) || AppState.Data.ShowDescriptions.Value);

        public string GroupByKey => Owner.ClassName;

        public bool GearWithIncompleteObjectives => (Definition.ItemCategoryHashes?.Contains(ArmourCategoryHash) ?? false) &&
                                          IsCompleted != true;

        public bool ShowInPursuits => BucketHash == PersuitsBucketHash || GearWithIncompleteObjectives;

        public string Title =>
            Definition?.SetData?.QuestLineName ?? Definition?.DisplayProperties?.Name ?? "No name";

        public Uri ImageUri => new Uri($"https://www.bungie.net{Definition?.DisplayProperties?.Icon ?? "/img/misc/missing_icon_d2.png"}");

        public string SortValue {
            get
            {
                var value = 100;

                if (IsCompleted)
                    value += 100;

                return value.ToString();
            }
        }

        public string Subtitle => Definition?.ItemTypeDisplayName ?? "Pursuit";

        public async Task<DestinyInventoryItemDefinition> PopulateDefinition()
        {
            Definition = await Definitions.GetInventoryItem(ItemHash);
            return Definition;
        }

        public static async Task<Item> ItemFromItemComponent(DestinyItemComponent item, DestinyProfileResponse profile, TrackableOwner ownerCharacter = default)
        {
            var uninstancedObjectivesData =
                ownerCharacter == null
                ?  new Dictionary<string, DestinyItemObjectivesComponent>()
                : profile.CharacterUninstancedItemComponents[ownerCharacter.CharacterComponent.CharacterId.ToString()].Objectives.Data;

            var objectives = new List<DestinyObjectiveProgress>();
            var itemInstanceId = item.ItemInstanceId.ToString();
            var itemHash = item.ItemHash.ToString();

            if (profile.ItemComponents.Objectives.Data.ContainsKey(itemInstanceId))
            {
                objectives.AddRange(profile.ItemComponents.Objectives.Data[itemInstanceId]?.Objectives);
            }

            if (item.ItemInstanceId.Equals(0) && uninstancedObjectivesData.ContainsKey(itemHash))
            {
                objectives.AddRange(uninstancedObjectivesData[itemHash].Objectives);
            }

            if (objectives.Count == 0)
            {
                return default;
            }

            var bounty = new Item()
            {
                ItemHash = item.ItemHash,
                ItemInstanceId = item.ItemInstanceId,
                BucketHash = item.BucketHash,
                Owner = ownerCharacter,
                Objectives = new List<Objective>()
            };
            
            await bounty.PopulateDefinition();

            foreach (var destinyQuestsDestinyObjectiveProgress in objectives)
            {
                var obj = new Objective { Progress = destinyQuestsDestinyObjectiveProgress };
                await obj.PopulateDefinition();
                bounty.Objectives.Add(obj);
            }

            return bounty;
        }

        public static async Task<List<Item>> ItemsFromProfile(DestinyProfileResponse profile, TrackableOwner activeCharacter)
        {
            var bounties = new List<Item>();

            async Task EachInventoryItem(DestinyItemComponent inventoryItem, TrackableOwner ownerCharacter = default)
            {
                var bounty = await ItemFromItemComponent(inventoryItem, profile, ownerCharacter);

                if (bounty != null && bounty.ShowInPursuits && bounty.Objectives?.Count > 0)
                {
                    bounties.Add(bounty);
                }
            }

            async Task EachCharacterInventory(string characterId, DestinyInventoryComponent inventory)
            {
                if (characterId != activeCharacter.CharacterId.ToString())
                    return;

                var character = await TrackableOwner.GetTrackableOwner(profile.Characters.Data[characterId]);

                foreach (var inventoryItem in inventory.Items)
                    await EachInventoryItem(inventoryItem, character);
            }

            foreach (var kv in profile.CharacterInventories.Data)
                await EachCharacterInventory(kv.Key, kv.Value);

            foreach (var kv in profile.CharacterEquipment.Data)
                await EachCharacterInventory(kv.Key, kv.Value);

            foreach (var inventoryItem in profile.ProfileInventory.Data.Items)
                await EachInventoryItem(inventoryItem);

            return bounties;
        }

        public void UpdateTo(ITrackable newTrackable)
        {
            if (!(newTrackable is Item newItem)) return;

            BucketHash = newItem.BucketHash;
            Definition = newItem.Definition;

            // If the amount of objectives stays the same, update them in place. Otherwise, outright replace them all.
            // Note: This doesn't handle if the count stays the same, but one objective is removed and other is added,
            // but I don't think that'll ever happen in real life
            if (Objectives.Count == newItem.Objectives.Count)
            {
                Objectives.ForEach(existingObjective =>
                {
                    var newObjective = newItem.Objectives.Find(v =>
                        v.Progress.ObjectiveHash == existingObjective.Progress.ObjectiveHash);

                    existingObjective?.UpdateTo(newObjective);
                });
            }
            else
            {
                Objectives = newItem.Objectives;
            }
        }

        public override string ToString()
        {
            return $"ITrackable.Item(ItemHash: {ItemHash}, ItemInstanceId: {ItemInstanceId}, Name: {DisplayProperties?.Name ?? "No name"})";
        }
    }
}
