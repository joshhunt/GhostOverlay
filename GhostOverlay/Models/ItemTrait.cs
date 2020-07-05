using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BungieNetApi.Model;

namespace GhostOverlay.Models
{
    class ItemTrait
    {
        private static List<DestinyDefinitionsTraitsDestinyTraitCategoryDefinition> allTraitCategoryDefinitions;
        
        private static readonly Dictionary<string, string> CustomTraitNames = new Dictionary<string, string>
        {
            {"inventory_filtering.quest", "All Quests"},
            {"inventory_filtering.bounty", "Bounties"},
            {"inventory_filtering.quest.featured", "Featured Quests"},
        };

        private static readonly Dictionary<string, Uri> CustomIcons = new Dictionary<string, Uri>
        {
            {"inventory_filtering.bounty", new Uri("ms-appx:///Assets/QuestTraitIcons/bounties.png")},
            {"inventory_filtering.quest", new Uri("ms-appx:///Assets/QuestTraitIcons/quests.png")},
            {"quest.new_light", new Uri("ms-appx:///Assets/QuestTraitIcons/new_light.png")},
            {"inventory_filtering.quest.featured", new Uri("ms-appx:///Assets/QuestTraitIcons/new_light.png")},
            {"quest.current_release", new Uri("ms-appx:///Assets/QuestTraitIcons/current_release.png")},
            {"quest.seasonal", new Uri("ms-appx:///Assets/QuestTraitIcons/seasonal.png")},
            {"quest.playlists", new Uri("ms-appx:///Assets/QuestTraitIcons/playlists.png")},
            {"quest.exotic", new Uri("ms-appx:///Assets/QuestTraitIcons/exotic.png")},
            {"quest.past", new Uri("ms-appx:///Assets/QuestTraitIcons/past.png")}
        };

        public string TraitId;
        public DestinyDefinitionsTraitsDestinyTraitDefinition Definition;
        public DestinyDefinitionsTraitsDestinyTraitCategoryDefinition TraitCategoryDefinition;
        public bool HasIcon => CustomIcons.ContainsKey(TraitId);
        public Uri IconUri => HasIcon ? CustomIcons[TraitId] : CommonHelpers.LocalFallbackIconUri;

        public string Name
        {
            get
            {
                var name = Definition?.DisplayProperties?.Name ?? "";
                var nearlyName = name == "" ? TraitId : name;
                return CustomTraitNames.ContainsKey(nearlyName) ? CustomTraitNames[TraitId] : nearlyName;
            }
        }

        public async Task<DestinyDefinitionsTraitsDestinyTraitDefinition> PopulateDefinition()
        {
            if (allTraitCategoryDefinitions == null)
            {
                allTraitCategoryDefinitions = await Definitions.GetAllTraitCategories();
            }

            TraitCategoryDefinition = allTraitCategoryDefinitions.FirstOrDefault(v => v.TraitIds.Contains(TraitId));

            if (TraitCategoryDefinition == null)
            {
                return default;
            }

            var index = TraitCategoryDefinition.TraitIds.IndexOf(TraitId);
            var traitHash = TraitCategoryDefinition.TraitHashes[index];

            Definition = await Definitions.GetTrait(traitHash);

            return Definition;
        }
    }
}
