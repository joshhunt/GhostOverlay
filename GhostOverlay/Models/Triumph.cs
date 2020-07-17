using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
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
        public string Subtitle => Definition?.CompletionInfo?.ScoreValue != null ? $"{Definition?.CompletionInfo?.ScoreValue} pts" : "";

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

            var characterRecords = characterIds.Select(characterId =>
            {
                var recordsForCharacter = profile.CharacterRecords.Data[characterId.ToString()];
                return recordsForCharacter.Records.GetValueOrDefault(triumphHash);
            }).Where(v => v != null).ToList();

            var completedCharacterRecord =
                characterRecords.FirstOrDefault(v =>
                    !v.State.HasFlag(DestinyRecordState.ObjectiveNotCompleted) ||
                    v.State.HasFlag(DestinyRecordState.RecordRedeemed));

            return completedCharacterRecord ?? characterRecords.FirstOrDefault();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void NotifyPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
