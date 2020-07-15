using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using BungieNetApi.Model;
using GhostSharp.BungieNetApi.Models;

namespace GhostOverlay.Models
{
    public enum DynamicTrackableType
    {
        None,
        CrucibleMap
    }

    abstract class DynamicTrackable : ITrackable
    {
        public bool IsCompleted { get; set; }
        public DestinyDisplayPropertiesDefinition DisplayProperties { get; set; }
        public List<Objective> Objectives { get; set; }
        public string Title { get; set; }
        public Uri ImageUri => new Uri($"https://www.bungie.net{DisplayProperties?.Icon ?? "/img/misc/missing_icon_d2.png"}");
        public TrackedEntry TrackedEntry { get; set; }
        public bool ShowDescription => true;

        public abstract string SortValue { get; }
        public string Subtitle => "";
        public abstract string GroupByKey { get; }
        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void NotifyPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    class CrucibleMapTrackable : DynamicTrackable
    {
        // crucible map
        public long CurrentActivityHash { get; set; }
        public DestinyActivityDefinition CurrentActivityDefinition {  get; set;  }

        // crucible mode (control, rumble, etc)
        public long CurrentActivityModeHash { get; set; }
        public DestinyActivityModeDefinition CurrentActivityModeDefinition { get; set; }

        public bool isInActivity => CurrentActivityDefinition != null && CurrentActivityModeDefinition != null;
        public bool isNotInActivity => !isInActivity;
        public Uri PGCRImageUri => CommonHelpers.BungieUri(CurrentActivityDefinition?.PgcrImage, CommonHelpers.FallbackPGCRImagePath);

        public Character OwnerCharacter;

        public override string GroupByKey => OwnerCharacter?.ClassName ?? "Insights";
        public override string SortValue => isInActivity ? "AAA" : "XXXXXXXXXXXXXX";

        public async Task PopulateDefinitions()
        {
            CurrentActivityDefinition = await Definitions.GetActivity(CurrentActivityHash);
            CurrentActivityModeDefinition = await Definitions.GetActivityMode(CurrentActivityModeHash);
        }

        public static async Task<CrucibleMapTrackable> CreateFromProfile(DestinyResponsesDestinyProfileResponse profile)
        {
            DateTime activityStart = DateTime.MinValue;
            string activeCharacterId = "";
            DestinyEntitiesCharactersDestinyCharacterActivitiesComponent currentActivitiesComponent = default;

            foreach (var (characterId, activitiesComponent) in profile.CharacterActivities.Data)
            {
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
            };

            if (profile.Characters.Data.ContainsKey(activeCharacterId))
            {
                tracker.OwnerCharacter = new Character { CharacterComponent = profile.Characters.Data[activeCharacterId] };
                await tracker.OwnerCharacter.PopulateDefinition();
            }
            
            await tracker.PopulateDefinitions();

            return tracker;
        }
    }
}
