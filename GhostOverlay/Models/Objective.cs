using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using GhostSharper.Models;

namespace GhostOverlay
{

    public class Objective : INotifyPropertyChanged
    {
        public DestinyObjectiveDefinition Definition =
            new DestinyObjectiveDefinition();

        private DestinyObjectiveProgress progressBacking;
        public DestinyObjectiveProgress Progress
        {
            get => progressBacking;
            set
            {
                if (!value.Equals(progressBacking))
                {
                    progressBacking = value;
                    OnPropertyChanged();

                    // TODO: do we have a better way than manually keeping track?
                    OnPropertyChanged($"Visibility");
                    OnPropertyChanged($"CompletionPercent");
                }
            }
        }

        public Visibility Visibility => (Progress.Progress == 0 && Progress.CompletionValue == 0)
            ? Visibility.Collapsed
            : Visibility.Visible;

        public double CompletionPercent
        {
            get
            {
                switch (Progress.CompletionValue)
                {
                    case 0 when Progress.Progress == 0:
                        return 0;
                    case 0 when Progress.Progress > 0:
                        return 100;
                    default:
                        return Math.Min(100, Math.Floor((double)Progress.Progress / Progress.CompletionValue * 100));
                }
            }

        }

        public async Task<DestinyObjectiveDefinition> PopulateDefinition()
        {
            Definition = await Definitions.GetObjective(Progress.ObjectiveHash);

            return Definition;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
