using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using GhostSharper.Models;
using Serilog;

namespace GhostOverlay.Models
{
    public class Triumph : ITrackable
    {
        public TrackedEntry TrackedEntry { get; set; }
        public DestinyRecordDefinition Definition;
        public DestinyRecordComponent Record;
        public List<Objective> Objectives { get; set; }
        public long Hash = 0;
        public DestinyDisplayPropertiesDefinition DisplayProperties =>
            Definition.DisplayProperties;

        public bool IsCompleted => Objectives?.TrueForAll(v => v.Progress.Complete) ?? false;
        public string GroupByKey => "Triumphs";

        public bool ShowDescription =>
            !IsCompleted && (TrackedEntry.ShowDescription || AppState.Data.ShowDescriptions.Value);

        public string SortValue => (IsCompleted ? "xxx_completed" : "");
        public string Subtitle => "Triumph";

        public string Title => Definition?.DisplayProperties?.Name ?? "No name";
        public Uri ImageUri => new Uri($"https://www.bungie.net{Definition?.DisplayProperties?.Icon ?? "/img/misc/missing_icon_d2.png"}");

        public async Task<DestinyRecordDefinition> PopulateDefinition()
        {
            Definition = await Definitions.GetRecord(Hash);
            return Definition;
        }

        public static DestinyRecordComponent FindRecordInProfileOrDefault(string triumphHash, DestinyProfileResponse profile)
        {
            var characterIds = profile?.Profile?.Data?.CharacterIds ?? new List<long>();

            if (profile?.CharacterRecords?.Data == null)
                return default;

            var profileRecord = profile.ProfileRecords.Data.Records.GetValueOrDefault(triumphHash);

            if (profileRecord != null)
                return profileRecord;

            foreach (var characterId in characterIds)
            {
                // TODO: we should probably return the most complete one, rather than the first we find?
                var recordsForCharacter = profile.CharacterRecords.Data[characterId.ToString()];
                var charRecord = recordsForCharacter.Records.GetValueOrDefault(triumphHash);

                if (charRecord != null)
                {
                    return charRecord;
                };
            }

            return default;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void NotifyPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
