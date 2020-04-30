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

        public async void PopulateDefinition()
        {
            var classHash = Convert.ToUInt32(CharacterComponent.ClassHash);
            ClassDefinition = await Definitions.GetClass(classHash);
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
            ObjectiveDefinition = await Definitions.GetObjective(hash);
        }
    }

    public interface ITriumphsViewChildren { }

    public class PresentationNode : ITriumphsViewChildren
    {
        public long PresentationNodeHash;
        public DestinyDefinitionsPresentationDestinyPresentationNodeDefinition Definition;
        public Uri ImageUri => new Uri($"https://www.bungie.net{Definition?.DisplayProperties?.Icon ?? "/img/misc/missing_icon_d2.png"}");
    }
}