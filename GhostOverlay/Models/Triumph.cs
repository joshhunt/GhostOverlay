using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GhostSharper.Models;

namespace GhostOverlay.Models
{
    public class Reward
    {
        public long ItemHash;
        public DestinyInventoryItemDefinition ItemDefinition { get; set; }
        public long Quantity = 1;

        public Uri ImageUri => new Uri($"https://www.bungie.net{ItemDefinition?.DisplayProperties?.Icon ?? "/img/misc/missing_icon_d2.png"}");

        public async Task<DestinyInventoryItemDefinition> PopulateDefinition()
        {
            ItemDefinition = await Definitions.GetInventoryItem(ItemHash);
            return ItemDefinition;
        }
    }

    public class Triumph : ITrackable
    {
        #pragma warning disable 67
        public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore 67


        private static readonly Logger Log = new Logger("Triumph");

        public static TrackableOwner StaticTriumphOwner = new TrackableOwner { DummyOwnerTitle = "Triumphs" };
        public static TrackableOwner StaticSeasonalChallengesOwner = new TrackableOwner { DummyOwnerTitle = "Seasonal Challenges" };

        public TrackedEntry TrackedEntry { get; set; }
        public DestinyRecordDefinition Definition;
        public DestinyRecordComponent Record;
        public List<Objective> Objectives { get; set; }
        public long Hash = 0;
        public List<Reward> Rewards = new List<Reward>();
        public DestinyDisplayPropertiesDefinition DisplayProperties =>
            Definition.DisplayProperties;

        public List<DestinyObjectiveProgress> ObjectiveProgresses
        {
            get
            {
                var objectives = Record?.Objectives != null
                    ? Record.Objectives.Select(v => v).ToList()
                    : new List<DestinyObjectiveProgress>();

                var intervalObjectives = (Record?.IntervalObjectives?.Count ?? 0) > 0
                    ? new List<DestinyObjectiveProgress>() { Record.IntervalObjectives.Last() }
                    : new List<DestinyObjectiveProgress>();

                objectives.AddRange(intervalObjectives);

                return objectives;
            }
        }

        public long Points
        {
            get
            {
                if (Definition == null)
                    return 0;

                var intervalPoints =
                    Definition.IntervalInfo?.IntervalObjectives?.Aggregate(0L,
                        (acc, x) => acc + x.IntervalScoreValue) ?? 0;

                var normalPoints = Definition.CompletionInfo?.ScoreValue ?? 0;

                return intervalPoints + normalPoints;
            }
        }

        public TrackableOwner Owner
        {
            get => TrackedEntry.Type == TrackedEntryType.SeasonalChallenge ? StaticSeasonalChallengesOwner : StaticTriumphOwner;
            set => throw new Exception("Don't set Owner on Triumphs");
        }

        public bool IsCompleted => Objectives?.TrueForAll(v => v.Progress.Complete) ?? false;

        public bool ShowDescription =>
            !IsCompleted && (TrackedEntry.ShowDescription || AppState.Data.ShowDescriptions.Value);

        public string SortValue => (IsCompleted ? "xxx_completed" : "");
        public string Subtitle => Points > 0 ? $"{Points} pts" : "";

        public string Title => Definition?.DisplayProperties?.Name ?? "No name";
        public Uri ImageUri => new Uri($"https://www.bungie.net{Definition?.DisplayProperties?.Icon ?? "/img/misc/missing_icon_d2.png"}");

        public async Task<DestinyRecordDefinition> PopulateDefinition()
        {
            Log.Info("Populating definition");
            Definition = await Definitions.GetRecord(Hash);
            await PopulateRewards();
            
            return Definition;
        }

        public async Task PopulateRewards()
        {
            Log.Info("Populating rewards");
            Rewards.RemoveAll(JustReturnAll);
            var rewardItems = Definition?.RewardItems ?? new List<DestinyItemQuantity>();
            Log.Info("There are {length} rewards on the definition {hash}", rewardItems, Definition?.Hash);
            Debug.WriteLine(Definition.RewardItems);

            foreach (var rewardItem in rewardItems)
            {
                Log.Info("Looking at item reward {ItemHash}", rewardItem.ItemHash);

                var reward = new Reward()
                    {ItemHash = rewardItem.ItemHash, Quantity = rewardItem.Quantity};
                await reward.PopulateDefinition();
                if (reward.ItemDefinition != null)
                {
                    Rewards.Add(reward);
                    Log.Info("added reward {Name}", reward.ItemDefinition.DisplayProperties.Name);
                } 
            }
        }

        // Search predicate returns true if a string ends in "saurus".
        private static bool JustReturnAll(Reward s)
        {
            return true;
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

        public void UpdateTo(ITrackable newTrackable)
        {
            if (!(newTrackable is Triumph newTriumph)) return;

            Definition = newTriumph.Definition;
            Record = newTriumph.Record;

            // If the amount of objectives stays the same, update them in place. Otherwise, outright replace them all.
            // Note: This doesn't handle if the count stays the same, but one objective is removed and other is added,
            // but I don't think that'll ever happen in real life
            if (Objectives.Count == newTriumph.Objectives.Count)
            {
                Objectives.ForEach(existingObjective =>
                {
                    var newObjective = newTriumph.Objectives.Find(v =>
                        v.Progress.ObjectiveHash == existingObjective.Progress.ObjectiveHash);

                    existingObjective?.UpdateTo(newObjective);
                });
            }
            else
            {
                Objectives = newTriumph.Objectives;
            }
        }
    }
}
