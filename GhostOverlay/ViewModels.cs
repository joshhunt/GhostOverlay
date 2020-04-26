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

namespace GhostOverlay
{
    public class TimerViewModel : INotifyPropertyChanged
    {
        public TimerViewModel()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += timer_Tick;
            timer.Start();
            startTime = DateTime.Now;
        }

        private DispatcherTimer timer;
        private DateTime startTime;
        public event PropertyChangedEventHandler PropertyChanged;
        public TimeSpan TimeFromStart => DateTime.Now - startTime;

        private void timer_Tick(object sender, object e)
        {
            RaisePropertyChanged("TimeFromStart");
        }

        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }


    public class RangeObservableCollection<T> : ObservableCollection<T>
    {
        private bool _suppressNotification = false;

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!_suppressNotification)
                base.OnCollectionChanged(e);
        }

        public void AddRange(IEnumerable<T> list)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            _suppressNotification = true;

            foreach (T item in list)
            {
                Add(item);
            }
            _suppressNotification = false;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }

    public class Character
    {
        public DestinyEntitiesCharactersDestinyCharacterComponent CharacterComponent;
        public DestinyDefinitionsDestinyClassDefinition ClassDefinition;

        public string ClassName =>
            ClassDefinition?.GenderedClassNamesByGenderHash[CharacterComponent.GenderHash.ToString()];

        public async Task PopulateDefinition()
        {
            var classHash = Convert.ToUInt32(CharacterComponent.ClassHash);
            ClassDefinition = await Definitions.GetClassDefinition(classHash);
        }
    }

    public class Bounty
    {
        public DestinyEntitiesItemsDestinyItemComponent Item;

        public DestinyDefinitionsDestinyInventoryItemDefinition ItemDefinition =
            new DestinyDefinitionsDestinyInventoryItemDefinition();

        public List<Objective> Objectives = new List<Objective>();
        public Character OwnerCharacter;
        public bool AllObjectivesComplete => Objectives?.TrueForAll(v => v.Progress.Complete) ?? false;

        public string Title =>
            ItemDefinition?.SetData?.QuestLineName ?? ItemDefinition?.DisplayProperties?.Name ?? "No name";

        [Obsolete("OwnerCharacterId is deprecated, use OwnerCharacter instead.")]
        public string OwnerCharacterId;

        public Uri ImageUri => new Uri($"https://www.bungie.net{ItemDefinition?.DisplayProperties?.Icon ?? "/img/misc/missing_icon_d2.png"}");

        public async void PopulateDefinition()
        {
            var hash = Convert.ToUInt32(Item.ItemHash);
            ItemDefinition = await Definitions.GetItemDefinition(hash);
        }

        public static Bounty BountyFromItemComponent(DestinyEntitiesItemsDestinyItemComponent item, DestinyResponsesDestinyProfileResponse profile, Character ownerCharacter)
        {
            var uninstancedObjectivesData = profile.CharacterUninstancedItemComponents[ownerCharacter.CharacterComponent.CharacterId.ToString()].Objectives.Data;
            var objectives = new List<DestinyQuestsDestinyObjectiveProgress>();
            var itemInstanceId = item.ItemInstanceId.ToString();
            var itemHash = item.ItemHash.ToString();

            if (profile.ItemComponents.Objectives.Data.ContainsKey(itemInstanceId))
            {
                objectives.AddRange(profile.ItemComponents.Objectives.Data[itemInstanceId]?.Objectives);
            }

            if (item.ItemInstanceId.Equals(0) && uninstancedObjectivesData.ContainsKey(itemHash))
            {
                objectives.AddRange(uninstancedObjectivesData[itemHash].Objectives);
            }

            var bounty = new Bounty()
            {
                Item = item,
                OwnerCharacter = ownerCharacter
                //AllObjectivesComplete = objectives.TrueForAll(v => v.Complete)
            };
            bounty.PopulateDefinition();

            foreach (var objectiveProgress in objectives)
            {
                var objective = new Objective { Progress = objectiveProgress };
                objective.PopulateDefinition();
                bounty.Objectives.Add(objective);
            }

            return bounty;
        }

        public static List<Bounty> BountiesFromProfile(DestinyResponsesDestinyProfileResponse profile, bool addCompletedBounties = true)
        {
            var bounties = new List<Bounty>();
        
            foreach (var inventoryKv in profile.CharacterInventories.Data)
            {
                var characterId = inventoryKv.Key;
                var inventory = inventoryKv.Value;

                var character = new Character { CharacterComponent = profile.Characters.Data[characterId] };
                _ = character.PopulateDefinition();

                foreach (var inventoryItem in inventory.Items)
                {
                    // Only from the Persuits bucket
                    if (inventoryItem.BucketHash != 1345459588) continue;

                    var bounty = BountyFromItemComponent(inventoryItem, profile, character);

                    if (addCompletedBounties || !bounty.AllObjectivesComplete)
                    {
                        bounties.Add(bounty);
                    }
                }
            }

            //bounties.Sort((a, b) => a.AllObjectivesComplete != b.AllObjectivesComplete ? ( a.AllObjectivesComplete ? 100 : 1 ) : 0);

            return bounties;
        }
    }

    public class Objective
    {
        public DestinyDefinitionsDestinyObjectiveDefinition ObjectiveDefinition =
            new DestinyDefinitionsDestinyObjectiveDefinition();

        public DestinyQuestsDestinyObjectiveProgress Progress;

        public double CompletionPercent =>
            Math.Min(100, Math.Floor((double)Progress.Progress / Progress.CompletionValue * 100));

        public async void PopulateDefinition()
        {
            var hash = Convert.ToUInt32(Progress.ObjectiveHash);
            ObjectiveDefinition = await Definitions.GetObjectiveDefinition(hash);
        }
    }
}