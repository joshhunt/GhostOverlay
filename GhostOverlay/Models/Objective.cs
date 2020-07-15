using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using BungieNetApi.Model;
using GhostSharp.BungieNetApi.Models;

namespace GhostOverlay
{

    public class Objective
    {
        public DestinyObjectiveDefinition Definition =
            new DestinyObjectiveDefinition();

        public DestinyQuestsDestinyObjectiveProgress Progress;

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
    }

}
