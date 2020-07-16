using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using GhostOverlay.Models;

namespace GhostOverlay
{
    public sealed partial class WidgetSettingsBountiesView : Page, ISubscriber<WidgetPropertyChanged>
    {
        private readonly WidgetStateChangeNotifier eventAggregator = new WidgetStateChangeNotifier();
        private static readonly Logger Log = new Logger("WidgetSettingsBountiesView");

        private readonly RangeObservableCollection<Item> Bounties = new RangeObservableCollection<Item>();
        private bool viewIsUpdating;
        private ItemTrait selectedTrait;

        public WidgetSettingsBountiesView()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is ItemTrait traitParam)
            {
                selectedTrait = traitParam;
                Log.Info($"navigated to with trait {selectedTrait.TraitId}");
            }

            eventAggregator.Subscribe(this);
            UpdateViewModel();
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            eventAggregator.Unsubscribe(this);
        }

        public void HandleMessage(WidgetPropertyChanged message)
        {
            Log.Debug($"HandleMessage {message}");

            switch (message)
            {
                case WidgetPropertyChanged.Profile:
                case WidgetPropertyChanged.DefinitionsPath:
                case WidgetPropertyChanged.ActiveCharacter:
                    UpdateViewModel();
                    break;

                case WidgetPropertyChanged.TrackedItems:
                    UpdateSelection();
                    break;
            }
        }

        private async void UpdateViewModel()
        {
            viewIsUpdating = true; 

            var profile = AppState.Data.Profile;

            if (profile?.CharacterInventories?.Data == null || !AppState.Data.DefinitionsLoaded || AppState.Data.ActiveCharacter == null)
            {
                return;
            }

            var bountiesForCharacter = (await Item.ItemsFromProfile(profile, AppState.Data.ActiveCharacter))
                .FindAll(v => (v.Definition?.TraitIds ?? new List<string>()).Contains(selectedTrait?.TraitId))
                .OrderBy(v => v.SortValue)
                .ToList();

            Bounties.Clear();
            Bounties.AddRange(bountiesForCharacter);

            _ = Task.Run(UpdateSelection);

            viewIsUpdating = false;
        }

        private async void UpdateSelection()
        {
            // TODO: Figure out why this Delay/Dispatch/Delay is needed to get SelectedItems to stick

            await Task.Delay(1);
            _ = BountiesGridView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                await Task.Delay(1);

                viewIsUpdating = true;
                this.BountiesGridView.SelectedItems.Clear();
                foreach (var bounty in Bounties)
                {
                    if (AppState.Data.IsTracked(bounty))
                    {
                        this.BountiesGridView.SelectedItems.Add(bounty);
                    }
                }

                viewIsUpdating = false;
            });
        }

        private void SelectedBountiesChanged(object sender, SelectionChangedEventArgs e)
        {
            if (viewIsUpdating) return;
            var copyOf = AppState.Data.TrackedEntries.ToList();
            
            foreach (var addedItem in e.AddedItems)
            {
                var bounty = addedItem as Item;
                copyOf.Add(TrackedEntry.FromItem(bounty));
            }

            foreach (var removedItem in e.RemovedItems)
            {
                var bounty = removedItem as Item;
                copyOf.RemoveAll(v => v.Matches(bounty));
            }

            AppState.Data.TrackedEntries = copyOf;
        }
    }
}