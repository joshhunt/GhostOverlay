using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BungieNetApi.Model;

namespace GhostOverlay.Models
{
    public class Item : ITrackable
    {

        public static long PersuitsBucketHash = 1345459588;

        public long ItemHash = 0;
        public long ItemInstanceId = 0;
        public TrackedEntry TrackedEntry { get; set; }

        public DestinyDefinitionsDestinyInventoryItemDefinition ItemDefinition =
            new DestinyDefinitionsDestinyInventoryItemDefinition();
        public DestinyDefinitionsCommonDestinyDisplayPropertiesDefinition DisplayProperties =>
            ItemDefinition.DisplayProperties;
        public List<Objective> Objectives { get; set; }
        public Character OwnerCharacter;
        public bool IsCompleted => Objectives?.TrueForAll(v => v.Progress.Complete) ?? false;

        public string GroupByKey => OwnerCharacter.ClassName;

        public string Title =>
            ItemDefinition?.SetData?.QuestLineName ?? ItemDefinition?.DisplayProperties?.Name ?? "No name";

        public Uri ImageUri => new Uri($"https://www.bungie.net{ItemDefinition?.DisplayProperties?.Icon ?? "/img/misc/missing_icon_d2.png"}");

        public async void PopulateDefinition()
        {
            var hash = Convert.ToUInt32(ItemHash);
            ItemDefinition = await Definitions.GetInventoryItem(hash);
        }

        public static Item ItemFromItemComponent(DestinyEntitiesItemsDestinyItemComponent item, DestinyResponsesDestinyProfileResponse profile, Character ownerCharacter)
        {
            var uninstancedObjectivesData = profile.CharacterUninstancedItemComponents[ownerCharacter.CharacterComponent.CharacterId.ToString()].Objectives.Data;
            var objectives = new List<DestinyQuestsDestinyObjectiveProgress>();
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
                return new Item();
            }

            var bounty = new Item()
            {
                ItemHash = item.ItemHash,
                ItemInstanceId = item.ItemInstanceId,
                OwnerCharacter = ownerCharacter
            };
            bounty.PopulateDefinition();

            bounty.Objectives = objectives?.ConvertAll(v =>
            {
                var obj = new Objective { Progress = v };
                obj.PopulateDefinition();
                return obj;
            });

            return bounty;
        }

        public static List<Item> ItemsFromProfile(DestinyResponsesDestinyProfileResponse profile, bool addCompletedBounties = true)
        {
            var bounties = new List<Item>();

            foreach (var inventoryKv in profile.CharacterInventories.Data)
            {
                var characterId = inventoryKv.Key;
                var inventory = inventoryKv.Value;

                var character = new Character { CharacterComponent = profile.Characters.Data[characterId] };
                character.PopulateDefinition();

                foreach (var inventoryItem in inventory.Items)
                {
                    if (inventoryItem.BucketHash != PersuitsBucketHash)
                        continue;

                    var bounty = ItemFromItemComponent(inventoryItem, profile, character);

                    if (bounty.Objectives?.Count > 0 && (addCompletedBounties || !bounty.IsCompleted))
                    {
                        bounties.Add(bounty);
                    }
                    else if (bounty.Objectives?.Count == 0)
                    {
                        bounty = null;
                    }
                }
            }

            return bounties;
        }
    }
}
