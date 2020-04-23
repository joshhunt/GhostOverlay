using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Navigation;
using Microsoft.Gaming.XboxGameBar;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace GhostOverlay
{
    public sealed partial class WidgetSettingsBountiesView : Page, ISubscriber<PropertyChanged>
    {
        private readonly RangeObservableCollection<Bounty> Bounties = new RangeObservableCollection<Bounty>();
        private string definitionsDbName = "<not loaded>";
        private bool isSettingSelectedBounties;
        private XboxGameBarWidget widget;

        public WidgetSettingsBountiesView()
        {
            InitializeComponent();
            var eventAggregator = new MyEventAggregator();
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
            Debug.WriteLine("\n\nWidgetSettings OnNavigatedTo");
            widget = e.Parameter as XboxGameBarWidget;

            if (widget != null)
            {
                widget.MaxWindowSize = new Size(1940, 2000);
                widget.MinWindowSize = new Size(200, 100);
                widget.HorizontalResizeSupported = true;
                widget.VerticalResizeSupported = true;
                widget.SettingsSupported = false;

                //widget.RequestedThemeChanged += Widget_RequestedThemeChanged;
                //Widget_RequestedThemeChanged(widget, null);

                _ = widget.TryResizeWindowAsync(new Size(1170, 790));
            }

            UpdateViewModel();
        }

        private void UpdateViewModel()
        {
            definitionsDbName = Path.GetFileNameWithoutExtension(AppState.WidgetData.DefinitionsPath ?? "");

            var profile = AppState.WidgetData.Profile;
            if (profile?.CharacterInventories?.Data == null) return;
            if (!AppState.WidgetData.DefinitionsLoaded) return;

            Bounties.Clear();
            Bounties.AddRange(Bounty.BountiesFromProfile(profile));

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

            for (var count = 0; count < Bounties.Count; count++)
            {
                var item = Bounties[count].Item;
                var isTracked = AppState.WidgetData.ItemIsTracked(item);
                if (isTracked)
                {
                    BountiesGridView.SelectRange(new ItemIndexRange(count, 1));
                }
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

                newTrackedItems.Add(new TrackedBounty
                    {ItemHash = item.ItemHash, ItemInstanceId = item.ItemInstanceId});
                seen.Add(id);
            }

            Debug.WriteLine($"Setting {newTrackedItems.Count} selected bounties");
            AppState.WidgetData.TrackedBounties = newTrackedItems;
        }

        //private async void Widget_RequestedThemeChanged(XboxGameBarWidget sender, object args)
        //{
        //    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
        //        () =>
        //        {
        //            Background = widget.RequestedTheme == ElementTheme.Dark
        //                ? WidgetBrushes.DarkBrush
        //                : WidgetBrushes.LightBrush;
        //        });
        //}

        private async void UpdateDefinitionsButton_OnClick(object sender, RoutedEventArgs e)
        {
            DefinitionsProgressRing.IsActive = true;
            await Definitions.CheckForLatestDefinitions();
            DefinitionsProgressRing.IsActive = false;
        }
    }
}