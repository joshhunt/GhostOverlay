using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using GhostOverlay.Models;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace GhostOverlay.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsInsightsView : Page, ISubscriber<WidgetPropertyChanged>, INotifyPropertyChanged
    {
        private readonly WidgetStateChangeNotifier notifier = new WidgetStateChangeNotifier();
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly Logger Log = new Logger("SettingsInsightsView");
        private bool viewIsUpdating = false;

        public SettingsInsightsView()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Log.Info("Tracked items:");
            foreach (var dataTrackedEntry in AppState.Data.TrackedEntries)
            {
                Log.Info("    {item}", dataTrackedEntry);
            }

            notifier.Subscribe(this);
            UpdateTracked();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            notifier.Unsubscribe(this);
        }

        public void HandleMessage(WidgetPropertyChanged message)
        {
            switch (message)
            {
                case WidgetPropertyChanged.TrackedItems:
                    UpdateTracked();
                    break;
            }
        }

        private void UpdateTracked()
        {
            viewIsUpdating = true;
            ItemsGridView.SelectedItems.Clear();

            var foundItem = AppState.Data.TrackedEntries.FirstOrDefault(v => v.Type == TrackedEntryType.DynamicTrackable && v.DynamicTrackableType == DynamicTrackableType.CrucibleMap);

            if (foundItem != null)
            {
                ItemsGridView.SelectedItems.Add(CrucibleMapGridViewItem);
            }

            Log.Info("crucible map isTracked {isTracked}", foundItem != null);
            viewIsUpdating = false;
        }

        private void SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Log.Info("SelectionChanged, viewIsUpdating: {viewIsUpdating}", viewIsUpdating);
            if (viewIsUpdating) return;

            var copyOf = AppState.Data.TrackedEntries.ToList();

            foreach (GridViewItem gridViewItem in e.AddedItems)
            {
                Log.Info("Adding {type}", gridViewItem.Tag);
                switch (gridViewItem.Tag)
                {
                    case "CrucibleMap":
                        copyOf.Add(TrackedEntry.FromDynamicTrackableType(DynamicTrackableType.CrucibleMap));
                        break;
                }
            }

            foreach (GridViewItem gridViewItem in e.RemovedItems)
            {
                Log.Info("Removing {type}", gridViewItem.Tag);
                switch (gridViewItem.Tag)
                {
                    case "CrucibleMap":
                        copyOf.RemoveAll(v => v.Matches(DynamicTrackableType.CrucibleMap));
                        break;
                }
            }

            Log.Info("Tracked items (copyOf):");
            foreach (var dataTrackedEntry in copyOf)
            {
                Log.Info("    {item}", dataTrackedEntry);
            }

            AppState.Data.TrackedEntries = copyOf;
        }

        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void OnPropertyChangedExplicit(string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
