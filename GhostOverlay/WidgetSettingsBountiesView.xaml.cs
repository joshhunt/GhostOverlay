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

        private void UpdateViewModel()
        {
            var profile = AppState.WidgetData.Profile;
            if (profile?.CharacterInventories?.Data == null) return;
            if (!AppState.WidgetData.DefinitionsLoaded) return;

            Bounties.Clear();
            Bounties.AddRange(Bounty.BountiesFromProfile(profile, addCompletedBounties: true));

            BountiesCollection.Source =
                from t in Bounties
                group t by t.OwnerCharacter
                into g
                select g;

            UpdateBountySelection();
        }

        private void UpdateBountySelection()
        {
            isSettingSelectedBounties = true;
            BountiesGridView.SelectedItem = null;
            BountiesGridView.SelectedIndex = -1;
            var trackedBounties = AppState.WidgetData.TrackedBounties;

            Debug.WriteLine($"UpdateBountySelection count {trackedBounties.Count}");

            for (var count = 0; count < Bounties.Count; count++)
            {
                Debug.WriteLine($"  for loop iter {count} starting");
                var item = Bounties[count].Item;

                Debug.WriteLine($"    checking ItemIsTracked");
                var isTracked = AppState.WidgetData.ItemIsTracked(item);

                Debug.WriteLine($"    possibly BountiesGridView.SelectRange");
                if (isTracked) BountiesGridView.SelectRange(new ItemIndexRange(count, 1));
            }
            isSettingSelectedBounties = false;
        }

        private void SelectedBountiesChanged(object sender, RoutedEventArgs e)
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
            this.EventAggregator.Publish("hello from second view");
        }
    }
}
