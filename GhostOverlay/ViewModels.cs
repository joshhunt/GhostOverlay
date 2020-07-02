using BungieNetApi.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.SpeechSynthesis;
using Windows.Networking;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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
        public static readonly string FallbackPGCRImagePath = "/img/theme/destiny/bgs/pgcrs/placeholder.jpg";
        public static readonly string FallbackIconImagePath = "/img/misc/missing_icon_d2.png";

        public static Uri LocalFallbackIconUri = new Uri("ms-appx:///Assets/QuestTraitIcons/missing_icon.png");
        public static Uri BungieUri(string baseUrl, string fallbackPath = default)
        {
            return new Uri($"http://www.bungie.net{baseUrl ?? fallbackPath ?? FallbackIconImagePath}");
        }

        public static async Task DoSoon(Control control, Action fn)
        {
            await Task.Delay(10);
            await control.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                await Task.Delay(10);
                fn();
            });
        }

        public static async Task TextToSpeech(string text)
        {
            // The media object for controlling and playing audio.
            MediaElement mediaElement = new MediaElement();

            // The object for controlling the speech synthesis engine (voice).
            var synth = new Windows.Media.SpeechSynthesis.SpeechSynthesizer();

            // Generate the audio stream from plain text.
            SpeechSynthesisStream stream = await synth.SynthesizeTextToStreamAsync(text);

            // Send the stream to the media object.
            mediaElement.SetSource(stream, stream.ContentType);
            mediaElement.Play();
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

        public Uri EmblemBackgroundUri => CommonHelpers.BungieUri(CharacterComponent?.EmblemBackgroundPath, "/common/destiny2_content/icons/9dc4f3283ee9f9fc3d3499e9f9f1756c.jpg");

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