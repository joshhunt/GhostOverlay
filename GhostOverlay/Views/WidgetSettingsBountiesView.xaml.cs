using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using GhostOverlay.Models;
using Microsoft.Gaming.XboxGameBar;

namespace GhostOverlay
{
    public sealed partial class WidgetSettingsBountiesView : Page, ISubscriber<PropertyChanged>
    {
        private readonly RangeObservableCollection<Item> Bounties = new RangeObservableCollection<Item>();
        private string definitionsDbName = "<not loaded>";
        private bool viewIsUpdating;
        private readonly MyEventAggregator eventAggregator = new MyEventAggregator();

        public WidgetSettingsBountiesView()
        {
            InitializeComponent();
        }

        public void HandleMessage(PropertyChanged message)
        {
            switch (message)
            {
                case PropertyChanged.Profile:
                case PropertyChanged.DefinitionsPath:
                    UpdateViewModel();
                    break;

                case PropertyChanged.TrackedBounties:
                    UpdateSelection();
                    break;
            }
            
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

        private void UpdateViewModel()
        {
            viewIsUpdating = true;
            definitionsDbName = Path.GetFileNameWithoutExtension(AppState.WidgetData.DefinitionsPath ?? "");

            var profile = AppState.WidgetData.Profile;
            if (profile?.CharacterInventories?.Data != null && AppState.WidgetData.DefinitionsLoaded)
            {
                Bounties.Clear();
                Bounties.AddRange(Item.ItemsFromProfile(profile));

                BountiesCollection.Source =
                    from t in Bounties
                    orderby t.IsCompleted
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

        private async void UpdateDefinitionsButton_OnClick(object sender, RoutedEventArgs e)
        {
            DefinitionsProgressRing.IsActive = true;
            await Definitions.CheckForLatestDefinitions();
            DefinitionsProgressRing.IsActive = false;
        }
    }
}