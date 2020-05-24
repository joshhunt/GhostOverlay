using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using BungieNetApi.Model;
using GhostOverlay.Models;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace GhostOverlay.Views
{
    /// <summary>
    ///     An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TriumphsListing : Page, ISubscriber<WidgetPropertyChanged>
    {
        private readonly MyEventAggregator eventAggregator = new MyEventAggregator();

        private long presentationNodeHash;
        private readonly RangeObservableCollection<Triumph> triumphs = new RangeObservableCollection<Triumph>();
        private bool viewIsUpdating;

        public TriumphsListing()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter != null) presentationNodeHash = (long) e.Parameter;

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
            //Debug.WriteLine($"[TriumphsListing] HandleMessage {message}");
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
            var profile = AppState.Data.Profile;

            if (!AppState.Data.DefinitionsLoaded || profile == null) return;

            triumphs.Clear();

            var presentationNode = await Definitions.GetPresentationNode(Convert.ToUInt32(presentationNodeHash));
            Debug.WriteLine($"thirdLevelNode: {presentationNode}");

            if (presentationNode == null) return;

            Debug.WriteLine($"thirdLevelNode.hash: {presentationNode.Hash}");
            foreach (var childRecord in presentationNode.Children.Records)
            {
                var recordDefinition = await Definitions.GetRecord(Convert.ToUInt32(childRecord.RecordHash));
                var triumph = new Triumph
                {
                    Definition = recordDefinition,
                    Hash = childRecord.RecordHash,
                    Objectives = new List<Objective>(),
                    Record = Triumph.FindRecordInProfile(childRecord.RecordHash.ToString(), profile)
                };

                var objectives = (triumph.Record?.IntervalObjectives?.Count ?? 0) > 0
                    ? triumph.Record.IntervalObjectives
                    : triumph.Record?.Objectives ?? new List<DestinyQuestsDestinyObjectiveProgress>();

                foreach (var objectiveProgress in objectives)
                {
                    var obj = new Objective {Progress = objectiveProgress};
                    await obj.PopulateDefinition();
                    triumph.Objectives.Add(obj);
                }

                if (triumph.Record != null)
                    triumphs.Add(triumph);
                else
                    Debug.WriteLine(
                        $"triumph {triumph.Definition.DisplayProperties.Name} skipped because its record is missing");
            }

            Debug.WriteLine($"actual triumphs: {triumphs.Count}");
        }

        private void UpdateSelection()
        {
            viewIsUpdating = true;

            TriumphsGrid.SelectedItems.Clear();
            foreach (var item in triumphs)
            {
                if (item is Triumph triumph && AppState.Data.IsTracked(triumph))
                {
                    TriumphsGrid.SelectedItems.Add(item);
                }
            }

            viewIsUpdating = false;
        }

        private void OnSelectedTriumphsChanged(object sender, SelectionChangedEventArgs e)
        {
            if (viewIsUpdating) return;

            var wasChanged = false;
            var copyOf = AppState.Data.TrackedEntries.ToList();

            foreach (var item in e.AddedItems)
                if (item is Triumph triumph)
                {
                    wasChanged = true;
                    var n = TrackedEntry.FromTriumph(triumph);
                    copyOf.Add(n);
                }

            foreach (var item in e.RemovedItems)
                if (item is Triumph triumph)
                {
                    wasChanged = true;
                    copyOf.RemoveAll(v => v.Matches(triumph));
                }

            if (wasChanged) AppState.Data.TrackedEntries = copyOf;
        }
    }
}