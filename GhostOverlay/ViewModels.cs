using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.SpeechSynthesis;
using Windows.Networking;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using GhostOverlay.Models;
using GhostSharper.Models;
using Serilog;

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
        public DestinyCharacterComponent CharacterComponent;
        public DestinyClassDefinition ClassDefinition { get; set; }
        public DestinyGenderDefinition GenderDefinition { get; set; }
        public DestinyRaceDefinition RaceDefinition { get; set; }

        public Uri EmblemBackgroundUri => CommonHelpers.BungieUri(CharacterComponent?.EmblemBackgroundPath, "/common/destiny2_content/icons/9dc4f3283ee9f9fc3d3499e9f9f1756c.jpg");

        public long CharacterId => CharacterComponent?.CharacterId ?? 0;

        public string ClassName =>
            ClassDefinition?.GenderedClassNamesByGenderHash[CharacterComponent.GenderHash.ToString()];

        public string GenderName => GenderDefinition?.DisplayProperties.Name ?? "Unknown";

        public string RaceName =>
            RaceDefinition?.GenderedRaceNamesByGenderHash[CharacterComponent.GenderHash.ToString()] ?? "Unknown";

        public async Task<DestinyClassDefinition> PopulateDefinition()
        {
            ClassDefinition = await Definitions.GetClass(CharacterComponent.ClassHash);
            return ClassDefinition;
        }

        public async Task<DestinyGenderDefinition> PopulateGenderDefinition()
        {
            GenderDefinition = await Definitions.GetGender(CharacterComponent.GenderHash);
            return GenderDefinition;
        }

        public async Task<DestinyRaceDefinition> PopulateRaceDefinition()
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

    public class PresentationNode
    {
        private static readonly Logger Log = new Logger("PresentationNode");
        private static readonly long triumphsCompletedObjectiveHash = 3230204557;
        private static string triumphsCompletedString;

        public long PresentationNodeHash;
        public DestinyPresentationNodeDefinition Definition;
        public Uri ImageUri => CommonHelpers.BungieUri(Definition?.DisplayProperties?.Icon);
        public PresentationNode ParentNode;

        public string Name => Definition?.DisplayProperties?.Name ?? "No name";
        public bool SkipSecondLevel;

        public Objective Objective { get; set; }
        public bool IsCompleted => (Objective?.Progress?.Progress ?? 1) >= (Objective?.Progress?.CompletionValue ?? 0);

        public static async Task<string> GetTriumphsCompletedString(
            DestinyObjectiveDefinition objDefinition)
        {
            if (triumphsCompletedString?.Length > 1)
            {
                return triumphsCompletedString;
            }

            if (objDefinition?.ProgressDescription?.Length > 1)
            {
                return objDefinition.ProgressDescription;
            }

            var objective = await Definitions.GetObjective(triumphsCompletedObjectiveHash);
            triumphsCompletedString = objective?.ProgressDescription ?? "Triumphs completed";

            return triumphsCompletedString;
        }

        public static DestinyPresentationNodeComponent FindProfilePresentationNode(long hash, DestinyProfileResponse profile)
        {
            var profilePresentationNodes = profile?.ProfilePresentationNodes?.Data?.Nodes;
            var characterPresentationNodes = profile?.CharacterPresentationNodes?.Data;
            var hashString = hash.ToString();

            if (profilePresentationNodes != null && profilePresentationNodes.ContainsKey(hashString))
            {
                return profilePresentationNodes[hashString];
            }

            if (characterPresentationNodes != null)
            {
                foreach (var (_, value) in characterPresentationNodes)
                {
                    if (value.Nodes.ContainsKey(hashString))
                    {
                        return value.Nodes[hashString];
                    }
                }
            }

            return default;
        }

        public static async Task<PresentationNode> FromHash(long hash, DestinyProfileResponse profile, PresentationNode parentNode)
        {
            var newNode = new PresentationNode
            {
                PresentationNodeHash = hash,
                ParentNode = parentNode,
                Definition = await Definitions.GetPresentationNode(hash),
            };

            Log.Info("Looking at {hash} {name}", hash, newNode.Definition.DisplayProperties.Name);

            var profileNodeData = FindProfilePresentationNode(hash, profile);

            if (profileNodeData != null && profileNodeData.Objective != null)
            {
                Log.Info("  {name}, found presentation node data", newNode.Definition.DisplayProperties.Name);

                var obj = new Objective { Progress = profileNodeData.Objective };
                await obj.PopulateDefinition();
                obj.Definition.ProgressDescription = await GetTriumphsCompletedString(obj.Definition);

                // Documentation says these values are the canonical ones
                obj.Progress.Progress = profileNodeData.ProgressValue;
                obj.Progress.CompletionValue = profileNodeData.CompletionValue;

                newNode.Objective = obj;
            }
            
            if (newNode.Objective == null && newNode.Definition.CompletionRecordHash != 0)
            {
                var completionRecord = Triumph.FindRecordInProfileOrDefault(newNode.Definition.CompletionRecordHash.ToString(), profile);
                if (completionRecord == null || !(completionRecord.Objectives?.Count > 0)) return newNode;

                if (completionRecord.Objectives.Count > 1)
                {
                    Log.Info("Completion record for PresentationNode {hash} has {count} objectives, using just the first", hash, completionRecord.Objectives.Count);
                }

                var obj = new Objective { Progress = completionRecord.Objectives[0] };
                await obj.PopulateDefinition();
                obj.Definition.ProgressDescription = await GetTriumphsCompletedString(obj.Definition);

                newNode.Objective = obj;
            }
 
            return newNode;
        }
    }
}