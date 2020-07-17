using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using GhostOverlay.Models;
using GhostSharper.Models;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace GhostOverlay.Views
{
    /// <summary>
    ///     An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TriumphsListing : Page, ISubscriber<WidgetPropertyChanged>
    {
        private static readonly Logger Log = new Logger("TriumphsListing");
        private readonly WidgetStateChangeNotifier eventAggregator = new WidgetStateChangeNotifier();

        private long presentationNodeHash;
        private readonly RangeObservableCollection<Triumph> triumphs = new RangeObservableCollection<Triumph>();
        private bool viewIsUpdating;
        private HashSet<long> PresentationNodeStack;

        public DestinyPresentationNodeDefinition PresentationNodeDef { get; set; }
        public string Heading { get; set; }

        public TriumphsListing()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter != null)
            {
                PresentationNodeStack = (HashSet<long>)e.Parameter;
                presentationNodeHash = PresentationNodeStack.Last();
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

            viewIsUpdating = true;
            triumphs.Clear();

            PresentationNodeDef = await Definitions.GetPresentationNode(presentationNodeHash);
            var stackDefs = new List<DestinyPresentationNodeDefinition>();
            foreach (var hash in PresentationNodeStack)
                stackDefs.Add(await Definitions.GetPresentationNode(hash));

            Heading = string.Join(" > ", stackDefs.Where(v => v != null).Select(v => v.DisplayProperties.Name));

            if (PresentationNodeDef == null) return;

            foreach (var childRecord in PresentationNodeDef.Children.Records)
            {
                var recordDefinition = await Definitions.GetRecord(childRecord.RecordHash);

                var triumph = new Triumph
                {
                    Definition = recordDefinition,
                    Hash = childRecord.RecordHash,
                    Objectives = new List<Objective>(),
                    Record = Triumph.FindRecordInProfileOrDefault(childRecord.RecordHash.ToString(), profile)
                };
                
                if (triumph.Record == null)
                {
                    Log.Info($"Triumph {childRecord.RecordHash} is missing its record from profile data");
                }

                var objectives = triumph.Record?.Objectives ?? new List<DestinyObjectiveProgress>();

                var hasIntervalObjectives = (triumph.Record?.IntervalObjectives?.Count ?? 0) > 0;
                var intervalObjectives = hasIntervalObjectives
                    ? triumph.Record.IntervalObjectives
                    : new List<DestinyObjectiveProgress>();
                var allIntervalsComplete = intervalObjectives.All(v => v.Complete);

                var remainingIntervalObjectives = intervalObjectives.Where(v => !v.Complete);

                objectives.AddRange(remainingIntervalObjectives);
                if (hasIntervalObjectives && allIntervalsComplete)
                    objectives.Add(intervalObjectives.LastOrDefault());

                foreach (var objectiveProgress in objectives)
                {
                    if (objectiveProgress == null) continue;

                    var obj = new Objective { Progress = objectiveProgress };
                    await obj.PopulateDefinition();
                    triumph.Objectives.Add(obj);
                }

                triumphs.Add(triumph);
            }

            viewIsUpdating = false;

            UpdateSelection();
        }

        private void UpdateSelection()
        {
            _ = CommonHelpers.DoSoon(TriumphsGrid, () =>
            {
                viewIsUpdating = true;

                TriumphsGrid.SelectedItems.Clear();
                Log.Debug("Selecting tracked triumphs");

                foreach (var item in triumphs)
                {
                    if (item is Triumph triumph && AppState.Data.IsTracked(triumph))
                    {
                        Log.Debug($" - Adding `{triumph.Definition.DisplayProperties.Name}` to Selected triumphs");
                        TriumphsGrid.SelectedItems.Add(item);
                    }
                }

                viewIsUpdating = false;
            });
        }

        private void OnSelectedTriumphsChanged(object sender, SelectionChangedEventArgs e)
        {
            Log.Info("OnSelectedTriumphsChanged called");
            if (viewIsUpdating) return;
            Log.Info("  OnSelectedTriumphsChanged running");

            var wasChanged = false;
            var copyOf = AppState.Data.TrackedEntries.ToList();

            foreach (var item in e.AddedItems)
                if (item is Triumph triumph)
                {
                    Log.Info($"    adding tracked triumph {triumph.DisplayProperties.Name}");
                    wasChanged = true;
                    var n = TrackedEntry.FromTriumph(triumph);
                    copyOf.Add(n);
                }

            foreach (var item in e.RemovedItems)
                if (item is Triumph triumph)
                {
                    Log.Info($"    removing tracked triumph {triumph.DisplayProperties.Name}");
                    wasChanged = true;
                    copyOf.RemoveAll(v => v.Matches(triumph));
                }

            if (wasChanged)
            {
                Log.Info("  tracked triumphs were changed.");
                AppState.Data.TrackedEntries = copyOf;
            }
        }
    }
}