using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using BungieNetApi.Model;
using ColorCode.Common;
using GhostOverlay.Models;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace GhostOverlay
{
    /// <summary>
    ///     An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class WidgetSettingsTriumphsView : Page, ISubscriber<PropertyChanged>
    {
        private readonly MyEventAggregator eventAggregator = new MyEventAggregator();

        private readonly RangeObservableCollection<ITriumphsViewChildren> Items =
            new RangeObservableCollection<ITriumphsViewChildren>();

        private long thisPresentationNodeHash = 1024788583; // root: 1024788583
        private bool viewIsUpdating;

        public WidgetSettingsTriumphsView()
        {
            InitializeComponent();
        }

        public void HandleMessage(PropertyChanged message)
        {
            Debug.WriteLine($"HandleMessage in triumphs view {message}");
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
            if (e.Parameter != null) thisPresentationNodeHash = (long) e.Parameter;
            eventAggregator.Subscribe(this);
            UpdateViewModel();
        }
        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            Debug.WriteLine("TriumphsView OnNavigatingFrom");
            base.OnNavigatingFrom(e);
            eventAggregator.Unsubscribe(this);
        }

        private async void UpdateViewModel()
        {
            viewIsUpdating = true;

            var profile = AppState.WidgetData.Profile;

            if (!AppState.WidgetData.DefinitionsLoaded) return;

            var rootNode = await Definitions.GetPresentationNode(Convert.ToUInt32(thisPresentationNodeHash));
            if (rootNode == null) return;

            Items.Clear();

            var characterIds = profile?.Profile?.Data?.CharacterIds ?? new List<long>();

            foreach (var childNode in rootNode.Children.PresentationNodes)
            {
                var childNodeDefinition =
                    await Definitions.GetPresentationNode(Convert.ToUInt32(childNode.PresentationNodeHash));
                var node = new PresentationNode
                    {PresentationNodeHash = childNode.PresentationNodeHash, Definition = childNodeDefinition};
                Items.Add(node);
            }

            foreach (var childRecord in rootNode.Children.Records)
            {
                var recordDefinition = await Definitions.GetRecord(Convert.ToUInt32(childRecord.RecordHash));
                var triumph = new Triumph {Definition = recordDefinition, Hash = childRecord.RecordHash};

                if (profile?.CharacterRecords?.Data != null)
                {
                    var isCharacterRecord = recordDefinition.Scope == 1;
                    var record = new DestinyComponentsRecordsDestinyRecordComponent();

                    if (isCharacterRecord)
                        foreach (var characterId in characterIds)
                        {
                            // TODO: we should probably return the most complete one, rather than the first we find?
                            var recordsForCharacter = profile.CharacterRecords.Data[characterId.ToString()];
                            record = recordsForCharacter.Records[childRecord.RecordHash.ToString()];

                            if (record != null) break;
                        }
                    else
                        record = profile.ProfileRecords.Data.Records[childRecord.RecordHash.ToString()];

                    triumph.Record = record;

                    var objectives = (record?.IntervalObjectives?.Count ?? 0) > 0
                        ? record.IntervalObjectives
                        : record?.Objectives;

                    triumph.Objectives = objectives?.ConvertAll(v =>
                    {
                        var obj = new Objective {Progress = v};
                        obj.PopulateDefinition();
                        return obj;
                    });
                }

                if (triumph.Record != null)
                    Items.Add(triumph);
                else
                    Debug.WriteLine(
                        $"triumph {triumph.Definition.DisplayProperties.Name} skipped because its record is missing");
            }

            Items.SortStable((a, b) =>
            {
                var aTriumph = a as Triumph;
                var bTriumph = b as Triumph;

                if (aTriumph != null && bTriumph != null)
                {
                    if (aTriumph.IsCompleted && bTriumph.IsCompleted ||
                        !aTriumph.IsCompleted && !bTriumph.IsCompleted) return 0;

                    if (aTriumph.IsCompleted && !bTriumph.IsCompleted) return 1;

                    if (!aTriumph.IsCompleted && bTriumph.IsCompleted) return -1;
                }

                return 0;
            });

            ItemsCollection.Source = Items;

            UpdateSelection();
            viewIsUpdating = false;
        }

        private void UpdateSelection()
        {
            viewIsUpdating = true;
            TriumphsGrid.SelectedItems.Clear();
            foreach (var item in Items)
            {
                if (item is Triumph triumph && AppState.WidgetData.IsTracked(triumph))
                {
                    TriumphsGrid.SelectedItems.Add(item);
                }
            }
            viewIsUpdating = false;
        }

        private void OnPresentationNodeClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is PresentationNode node)
                Frame.Navigate(typeof(WidgetSettingsTriumphsView), node.PresentationNodeHash);
        }

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (viewIsUpdating) return;
            var wasChanged = false;
            var copyOf = AppState.WidgetData.TrackedEntries.ToList();

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

            if (wasChanged) AppState.WidgetData.TrackedEntries = copyOf;
        }
    }

    public class TriumphsViewTemplateSelector : DataTemplateSelector
    {
        public DataTemplate PresentationNode { get; set; }
        public DataTemplate Triumph { get; set; }

        private DataTemplate Select(object item)
        {
            if (item is PresentationNode) return PresentationNode;
            if (item is Triumph) return Triumph;
            throw new NotImplementedException();
        }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            return Select(item);
        }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            return Select(item);
        }
    }

    public class TriumphViewGridItemStyleSelector : StyleSelector
    {
        public Style PresentationNode { get; set; }
        public Style Triumph { get; set; }

        protected override Style SelectStyleCore(object item, DependencyObject container)
        {
            if (item is PresentationNode) return PresentationNode;
            if (item is Triumph) return Triumph;
            throw new NotImplementedException();
        }
    }
}