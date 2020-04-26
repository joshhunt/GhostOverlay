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
using Microsoft.Gaming.XboxGameBar;

namespace GhostOverlay
{
    public sealed partial class WidgetSettingsBountiesView : Page, ISubscriber<PropertyChanged>
    {
        private readonly RangeObservableCollection<Bounty> Bounties = new RangeObservableCollection<Bounty>();
        private string definitionsDbName = "<not loaded>";
        private bool isSettingSelectedBounties;
        private XboxGameBarWidget widget;
        private readonly MyEventAggregator eventAggregator = new MyEventAggregator();

        public WidgetSettingsBountiesView()
        {
            InitializeComponent();
            eventAggregator.Subscribe(this);
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

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            widget = e.Parameter as XboxGameBarWidget;

            if (widget != null)
            {
                widget.MaxWindowSize = new Size(1940, 2000);
                widget.MinWindowSize = new Size(200, 100);
                widget.HorizontalResizeSupported = true;
                widget.VerticalResizeSupported = true;
                widget.SettingsSupported = false;

                widget.RequestedThemeChanged += Widget_RequestedThemeChanged;
                Widget_RequestedThemeChanged(widget, null);

                _ = widget.TryResizeWindowAsync(new Size(1170, 790));
            }

            AppState.WidgetData.ScheduleProfileUpdates();
            UpdateViewModel();
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            eventAggregator.Subscribe(this);
            AppState.WidgetData.UnscheduleProfileUpdates();
        }

        private void UpdateViewModel()
        {
            isSettingSelectedBounties = true;
            definitionsDbName = Path.GetFileNameWithoutExtension(AppState.WidgetData.DefinitionsPath ?? "");

            var profile = AppState.WidgetData.Profile;
            if (profile?.CharacterInventories?.Data != null && AppState.WidgetData.DefinitionsLoaded)
            {
                Bounties.Clear();
                Bounties.AddRange(Bounty.BountiesFromProfile(profile));

                BountiesCollection.Source =
                    from t in Bounties
                    orderby t.AllObjectivesComplete
                    group t by t.OwnerCharacter
                    into g
                    select g;

                UpdateBountySelection();
            }

            isSettingSelectedBounties = false;
        }

        private void UpdateBountySelection()
        {
            foreach (var bounty in Bounties)
            {
                var isTracked = AppState.WidgetData.ItemIsTracked(bounty.Item);
                if (isTracked)
                {
                    this.BountiesGridView.SelectedItems.Add(bounty);
                }
                
            }
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

                newTrackedItems.Add(new TrackedBounty
                    {ItemHash = item.ItemHash, ItemInstanceId = item.ItemInstanceId});
                seen.Add(id);
            }

            Debug.WriteLine($"Setting {newTrackedItems.Count} selected bounties");
            AppState.WidgetData.TrackedBounties = newTrackedItems;
        }


        private async void Widget_RequestedThemeChanged(XboxGameBarWidget sender, object args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () =>
                {
                    Background = widget.RequestedTheme == ElementTheme.Dark
                        ? new SolidColorBrush(Color.FromArgb(255, 0, 0, 0))
                        : new SolidColorBrush(Color.FromArgb(255, 76, 76, 76));
                });
        }

        private async void UpdateDefinitionsButton_OnClick(object sender, RoutedEventArgs e)
        {
            DefinitionsProgressRing.IsActive = true;
            await Definitions.CheckForLatestDefinitions();
            DefinitionsProgressRing.IsActive = false;
        }
    }
}