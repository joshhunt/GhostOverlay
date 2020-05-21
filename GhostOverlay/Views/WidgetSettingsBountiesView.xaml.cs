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
            switch (message)
            {
                case WidgetPropertyChanged.Profile:
                case WidgetPropertyChanged.DefinitionsPath:
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

            var profile = AppState.WidgetData.Profile;
            if (profile?.CharacterInventories?.Data != null && AppState.WidgetData.DefinitionsLoaded)
            {
                Bounties.Clear();
                Bounties.AddRange(await Item.ItemsFromProfile(profile));

                BountiesCollection.Source =
                    from t in Bounties
                    orderby t.SortValue
                    group t by t.OwnerCharacter
                    into g
                    select g;

                UpdateSelection();
            }

            viewIsUpdating = false;
        }

        private void UpdateSelection()
        {
            viewIsUpdating = true;
            this.BountiesGridView.SelectedItems.Clear();
            foreach (var bounty in Bounties)
            {
                if (AppState.WidgetData.IsTracked(bounty))
                {
                    this.BountiesGridView.SelectedItems.Add(bounty);
                }
            }
            viewIsUpdating = false;
        }

        private void SelectedBountiesChanged(object sender, SelectionChangedEventArgs e)
        {
            if (viewIsUpdating) return;
            var copyOf = AppState.WidgetData.TrackedEntries.ToList();
            
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

            AppState.WidgetData.TrackedEntries = copyOf;
        }
    }
}