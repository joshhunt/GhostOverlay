using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel.Channels;
using System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Navigation;
using BungieNetApi.Model;
using System.Threading.Tasks;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace GhostOverlay
{
    public sealed partial class WidgetSettingsBountiesView : Page, ISubscriber<PropertyChanged>
    {
        private RangeObservableCollection<Bounty> Bounties = new RangeObservableCollection<Bounty>();
        private bool isSettingSelectedBounties = false;
        private MyEventAggregator EventAggregator;

        public WidgetSettingsBountiesView()
        {
            this.InitializeComponent();
            this.EventAggregator = new MyEventAggregator();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Debug.WriteLine("Bounties page OnNavigatedTo");
            UpdateViewModel();
        }

        public void HandleMessage(PropertyChanged message)
        {
            switch (message)
            {
                case PropertyChanged.Profile:
                case PropertyChanged.DefinitionsPath:
                    UpdateViewModel();
                    break;
            }
        }

        private async void UpdateViewModel()
        {
            Debug.WriteLine("UpdateViewModel top");
            var profile = AppState.WidgetData.Profile;
            if (profile?.CharacterInventories?.Data == null) return;
            if (!AppState.WidgetData.DefinitionsLoaded) return;

            Debug.WriteLine("About to clear Bounties...");
            Bounties.Clear();
            Debug.WriteLine("About to Bounties.AddRange...");
            Bounties.AddRange(Bounty.BountiesFromProfile(profile, addCompletedBounties: true));


            Debug.WriteLine("About to BountiesCollection.Dispatcher...");
            await BountiesCollection.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                Debug.WriteLine("Inside BountiesCollection.Dispatcher...");
                BountiesCollection.Source =
                    from t in Bounties
                    group t by t.OwnerCharacter
                    into g
                    select g;
                Debug.WriteLine("BountiesCollection.Dispatcher is done...");
            });

            Debug.WriteLine("About to UpdateBountySelection...");
            await UpdateBountySelection();
            Debug.WriteLine("UpdateBountySelection is done");
        }

        private async Task UpdateBountySelection()
        {
            await BountiesGridView.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                isSettingSelectedBounties = true;
                BountiesGridView.SelectedItem = null;
                BountiesGridView.SelectedIndex = -1;
                var trackedBounties = AppState.WidgetData.TrackedBounties;

                Debug.WriteLine($"UpdateBountySelection count {trackedBounties.Count}");

                for (var count = 0; count < Bounties.Count; count++)
                {
                    var item = Bounties[count].Item;
                    var isTracked = AppState.WidgetData.ItemIsTracked(item);
                    if (isTracked) BountiesGridView.SelectRange(new ItemIndexRange(count, 1));
                }
                isSettingSelectedBounties = false;
            });

            return;
        }

        private async void SelectedBountiesChanged(object sender, RoutedEventArgs e)
        {
            await BountiesGridView.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                var senderGridView = sender as GridView;

                if (isSettingSelectedBounties || senderGridView == null) return;

                var seen = new List<long>();
                var newTrackedItems = new List<TrackedBounty>();

                foreach (var entry in senderGridView.SelectedItems)
                {
                    var item = (entry as Bounty)?.Item;
                    if (item == null) continue;

                    var id = item.ItemInstanceId != 0 ? item.ItemInstanceId : item.ItemHash;
                   if (seen.Contains(id)) continue;

                   newTrackedItems.Add(new TrackedBounty { ItemHash = item.ItemHash, ItemInstanceId = item.ItemInstanceId });
                  seen.Add(id);
                }
    
                Debug.WriteLine($"Setting {newTrackedItems.Count} selected bounties");
                AppState.WidgetData.TrackedBounties = newTrackedItems;
            });

            return;
        }
    }
}
