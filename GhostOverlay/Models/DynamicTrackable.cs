using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using GhostSharper.Models;

namespace GhostOverlay.Models
{
    public enum DynamicTrackableType
    {
        None,
        CrucibleMap
    }

    abstract class DynamicTrackable : ITrackable
    {
        #pragma warning disable 67
        public event PropertyChangedEventHandler PropertyChanged;
        #pragma warning restore 67

        public bool IsCompleted { get; set; }
        public DestinyDisplayPropertiesDefinition DisplayProperties { get; set; }
        public TrackableOwner Owner { get; set; }
        public List<Objective> Objectives { get; set; }
        public string Title { get; set; }
        public Uri ImageUri => new Uri($"https://www.bungie.net{DisplayProperties?.Icon ?? "/img/misc/missing_icon_d2.png"}");
        public TrackedEntry TrackedEntry { get; set; }
        public bool ShowDescription => true;

        public abstract string SortValue { get; }
        public string Subtitle => "";

        public abstract void UpdateTo(ITrackable item);
    }

    class CrucibleMapTrackable : DynamicTrackable
    {
        private static readonly Logger Log = new Logger("DynamicTrackable");

        // crucible map
        public long CurrentActivityHash { get; set; }
        public DestinyActivityDefinition CurrentActivityDefinition {  get; set;  }

        // crucible mode (control, rumble, etc)
        public long CurrentActivityModeHash { get; set; }
        public DestinyActivityModeDefinition CurrentActivityModeDefinition { get; set; }

        public bool isInActivity => CurrentActivityDefinition != null && CurrentActivityModeDefinition != null;
        public bool isNotInActivity => !isInActivity;
        public Uri PGCRImageUri => CommonHelpers.BungieUri(CurrentActivityDefinition?.PgcrImage, CommonHelpers.FallbackPGCRImagePath);

        public new TrackableOwner Owner { get; set; }

        public override string SortValue => isInActivity ? "AAA" : "XXXXXXXXXXXXXX";

        public async Task PopulateDefinitions()
        {
            CurrentActivityDefinition = await Definitions.GetActivity(CurrentActivityHash);
            CurrentActivityModeDefinition = await Definitions.GetActivityMode(CurrentActivityModeHash);
        }

        public override void UpdateTo(ITrackable newTrackable)
        {
            if (!(newTrackable is CrucibleMapTrackable newCrucibleMapTrackable)) return;

            CurrentActivityHash = newCrucibleMapTrackable.CurrentActivityHash;
            CurrentActivityDefinition = newCrucibleMapTrackable.CurrentActivityDefinition;

            CurrentActivityModeHash = newCrucibleMapTrackable.CurrentActivityModeHash;
            CurrentActivityModeDefinition = newCrucibleMapTrackable.CurrentActivityModeDefinition;
        }

        public static async Task<CrucibleMapTrackable> CreateFromProfile(DestinyProfileResponse profile,
            TrackedEntry trackedEntry)
        {
            var activeCharacterId = "";
            DateTime activityStart = DateTime.MinValue;
            DestinyCharacterActivitiesComponent currentActivitiesComponent = default;

            foreach (var (characterId, activitiesComponent) in profile.CharacterActivities.Data)
            {
                // TODO: validate dates are correct
                Log.Info("TODO: validate dates are correct: DateActivityStarted: {DateActivityStarted}, activityStart: {activityStart}", activitiesComponent.DateActivityStarted, activityStart);

                if (activitiesComponent.CurrentActivityHash != 0 && activitiesComponent.DateActivityStarted > activityStart)
                {
                    activeCharacterId = characterId;
                    currentActivitiesComponent = activitiesComponent;
                    activityStart = activitiesComponent.DateActivityStarted;
                }
            }

            if (currentActivitiesComponent == null)
            {
                return new CrucibleMapTrackable();
            }

            var tracker = new CrucibleMapTrackable()
            {
                CurrentActivityHash = currentActivitiesComponent.CurrentActivityHash,
                CurrentActivityModeHash = currentActivitiesComponent.CurrentActivityModeHash,
                TrackedEntry = trackedEntry,
            };

            if (profile.Characters.Data.ContainsKey(activeCharacterId))
            {
                tracker.Owner = await TrackableOwner.GetTrackableOwner(profile.Characters.Data[activeCharacterId]);
            }
            
            await tracker.PopulateDefinitions();

            return tracker;
        }
    }
}
