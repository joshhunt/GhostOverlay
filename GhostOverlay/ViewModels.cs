using BungieNetApi.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using GhostOverlay.Models;

namespace GhostOverlay
{
    [Flags]
    public enum RecordState
    {
        None = 0b_0000_0000,  // 0
        RecordRedeemed = 0b_0000_0001,  // 1
        RewardUnavailable = 0b_0000_0010,  // 2
        ObjectiveNotCompleted = 0b_0000_0100,  // 4
        Obscured = 0b_0000_1000,  // 8
        Invisible = 0b_0001_0000,  // 16
        EntitlementUnowned = 0b_0010_0000,  // 32
        CanEquipTitle = 0b_0100_0000  // 64
    }

    public class CommonHelpers
    {
        public static Uri BungieUri(string baseUrl)
        {
            return new Uri($"http://www.bungie.net{baseUrl ?? "/img/misc/missing_icon_d2.png"}");
        }
    }

    public class RangeObservableCollection<T> : ObservableCollection<T>
    {
        private bool suppressNotification = false;

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!suppressNotification)
                base.OnCollectionChanged(e);
        }

        public void AddRange(IEnumerable<T> list)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            suppressNotification = true;

            foreach (T item in list)
            {
                Add(item);
            }
            suppressNotification = false;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }

    public class Character
    {
        public DestinyEntitiesCharactersDestinyCharacterComponent CharacterComponent;
        public DestinyDefinitionsDestinyClassDefinition ClassDefinition { get; set; }
        public DestinyDefinitionsDestinyGenderDefinition GenderDefinition { get; set; }
        public DestinyDefinitionsDestinyRaceDefinition RaceDefinition { get; set; }

        public Uri EmblemBackgroundUri => CommonHelpers.BungieUri(CharacterComponent?.EmblemBackgroundPath);

        public long CharacterId => CharacterComponent?.CharacterId ?? 0;

        public string ClassName =>
            ClassDefinition?.GenderedClassNamesByGenderHash[CharacterComponent.GenderHash.ToString()];

        public string GenderName => GenderDefinition?.DisplayProperties.Name ?? "Unknown";

        public string RaceName =>
            RaceDefinition?.GenderedRaceNamesByGenderHash[CharacterComponent.GenderHash.ToString()] ?? "Unknown";

        public async Task<DestinyDefinitionsDestinyClassDefinition> PopulateDefinition()
        {
            ClassDefinition = await Definitions.GetClass(CharacterComponent.ClassHash);
            return ClassDefinition;
        }

        public async Task<DestinyDefinitionsDestinyGenderDefinition> PopulateGenderDefinition()
        {
            GenderDefinition = await Definitions.GetGender(CharacterComponent.GenderHash);
            return GenderDefinition;
        }

        public async Task<DestinyDefinitionsDestinyRaceDefinition> PopulateRaceDefinition()
        {
            RaceDefinition = await Definitions.GetRace(CharacterComponent.RaceHash);
            return RaceDefinition;
        }

        public async Task PopulatedExtendedDefinitions()
        {
            var tasks = new List<Task>
            {
                PopulateDefinition(),
                PopulateGenderDefinition(),
                PopulateRaceDefinition()
            };

            await Task.WhenAll(tasks);
        }
    }

    public class Objective
    {
        public DestinyDefinitionsDestinyObjectiveDefinition Definition =
            new DestinyDefinitionsDestinyObjectiveDefinition();

        public DestinyQuestsDestinyObjectiveProgress Progress;

        public double CompletionPercent =>
            Math.Min(100, Math.Floor((double)Progress.Progress / Progress.CompletionValue * 100));

        public async Task<DestinyDefinitionsDestinyObjectiveDefinition> PopulateDefinition()
        {
            Definition = await Definitions.GetObjective(Progress.ObjectiveHash);

            return Definition;
        }
    }

    public interface ITriumphsViewChildren { }

    public class PresentationNode : ITriumphsViewChildren
    {
        public long PresentationNodeHash;
        public DestinyDefinitionsPresentationDestinyPresentationNodeDefinition Definition;
        public Uri ImageUri => CommonHelpers.BungieUri(Definition?.DisplayProperties?.Icon);
        public PresentationNode ParentNode;

        public String Name => Definition?.DisplayProperties?.Name ?? "No name";
    }
}