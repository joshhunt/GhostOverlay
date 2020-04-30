using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public string Title => Definition.DisplayProperties.Name;
        public Uri ImageUri => new Uri($"https://www.bungie.net{Definition?.DisplayProperties?.Icon ?? "/img/misc/missing_icon_d2.png"}");

        public async Task<DestinyDefinitionsRecordsDestinyRecordDefinition> PopulateDefinition()
        {
            Definition = await Definitions.GetRecord(Convert.ToUInt32(Hash));
            return Definition;
        }
    }
}
