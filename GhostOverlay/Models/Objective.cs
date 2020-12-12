﻿using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using GhostSharper.Models;

namespace GhostOverlay
{

    public class Objective : INotifyPropertyChanged
    {
#pragma warning disable 67
        public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore 67

        public DestinyObjectiveDefinition Definition =
            new DestinyObjectiveDefinition();

        public DestinyObjectiveProgress Progress { get; set; }

        public Visibility Visibility => (Progress == null || Progress.Progress == default || (Progress.Progress == 0 && Progress.CompletionValue == 0))
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

        public void UpdateTo(Objective newObjective)
        {
            Progress = newObjective.Progress;
            Definition = newObjective.Definition;
        }
    }

}
