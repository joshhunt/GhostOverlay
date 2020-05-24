using System.Diagnostics;
using System.Linq;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using GhostOverlay.Models;

namespace GhostOverlay
{
    public sealed partial class WidgetSettingsBountiesView : Page, ISubscriber<WidgetPropertyChanged>
    {
        private readonly RangeObservableCollection<Item> Bounties = new RangeObservableCollection<Item>();
        private bool viewIsUpdating;
        private readonly MyEventAggregator eventAggregator = new MyEventAggregator();

        public WidgetSettingsBountiesView()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            eventAggregator.Subscribe(this);
            UpdateViewModel();
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            Debug.WriteLine("BountiesView OnNavigatingFrom");
            base.OnNavigatingFrom(e);
            eventAggregator.Unsubscribe(this);
        }

        public void HandleMessage(WidgetPropertyChanged message)
        {
            Log($"HandleMessage {message}");

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

            Log($"ActiveCharacter {AppState.Data.ActiveCharacter.CharacterId}");

            var bountiesForCharacter = await Item.ItemsFromProfile(profile, AppState.Data.ActiveCharacter);

            Bounties.Clear();
            Bounties.AddRange(bountiesForCharacter.OrderBy(v => v.SortValue));

            UpdateSelection();

            viewIsUpdating = false;
        }

        private void UpdateSelection()
        {
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



        private void Log(string message)
        {
            Debug.WriteLine($"[WidgetSettingsBountiesview] {message}");
        }
    }
}