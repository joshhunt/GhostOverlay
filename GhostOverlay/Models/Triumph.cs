using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using BungieNetApi.Model;

namespace GhostOverlay.Models
{
    public class Triumph : ITriumphsViewChildren, ITrackable
    {
        public TrackedEntry TrackedEntry { get; set; }
        public DestinyDefinitionsRecordsDestinyRecordDefinition Definition;
        public DestinyComponentsRecordsDestinyRecordComponent Record;
        public List<Objective> Objectives { get; set; }
        public long Hash = 0;
        public DestinyDefinitionsCommonDestinyDisplayPropertiesDefinition DisplayProperties =>
            Definition.DisplayProperties;

        public bool IsCompleted => Objectives?.TrueForAll(v => v.Progress.Complete) ?? false;
        public string GroupByKey => "Triumphs";

        public string SortValue => (IsCompleted ? "xxx_completed" : "") +
                                   (Definition?.ParentNodeHashes?[0].ToString() ?? "");

        public string Title => Definition.DisplayProperties.Name;
        public Uri ImageUri => new Uri($"https://www.bungie.net{Definition?.DisplayProperties?.Icon ?? "/img/misc/missing_icon_d2.png"}");

        public async Task<DestinyDefinitionsRecordsDestinyRecordDefinition> PopulateDefinition()
        {
            Definition = await Definitions.GetRecord(Hash);
            return Definition;
        }

        public static DestinyComponentsRecordsDestinyRecordComponent FindRecordInProfile(string triumphHash, DestinyResponsesDestinyProfileResponse profile)
        {
            var characterIds = profile?.Profile?.Data?.CharacterIds ?? new List<long>();
            DestinyComponentsRecordsDestinyRecordComponent record;

            if (profile?.CharacterRecords?.Data == null)
            {
                return default;
            }

            foreach (var characterId in characterIds)
            {
                // TODO: we should probably return the most complete one, rather than the first we find?
                var recordsForCharacter = profile.CharacterRecords.Data[characterId.ToString()];
                recordsForCharacter.Records.TryGetValue(triumphHash, out record);

                if (record != null)
                {
                    break;
                };
            }

            profile.ProfileRecords.Data.Records.TryGetValue(triumphHash, out record);

            return record;
        }
    }
}
