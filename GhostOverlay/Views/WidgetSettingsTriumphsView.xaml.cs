using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using GhostOverlay.Views;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace GhostOverlay
{
    public sealed partial class WidgetSettingsTriumphsView : Page, ISubscriber<WidgetPropertyChanged>, INotifyPropertyChanged
    {
        private static readonly Logger Log = new Logger("WidgetSettingsTriumphsView");
        private readonly WidgetStateChangeNotifier notifier = new WidgetStateChangeNotifier();
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly RangeObservableCollection<PresentationNode> secondLevelNodes =
            new RangeObservableCollection<PresentationNode>();

        private readonly RangeObservableCollection<PresentationNode> thirdLevelNodes =
            new RangeObservableCollection<PresentationNode>();

        private bool SkipSecondLevel = false;

        // cleaned up stuff
        private PresentationNode _selectedTopLevelNode;
        private PresentationNode _selectedSecondLevelNode;
        private PresentationNode _selectedThirdLevelNode;

        private PresentationNode SelectedTopLevelNode
        {
            get => _selectedTopLevelNode;
            set
            {
                _selectedTopLevelNode = value;
                OnPropertyChanged();
            }
        }

        private PresentationNode SelectedSecondLevelNode
        {
            get => _selectedSecondLevelNode;
            set
            {
                _selectedSecondLevelNode = value;
                OnPropertyChanged();
            }
        }

        private PresentationNode SelectedThirdLevelNode
        {
            get => _selectedThirdLevelNode;
            set
            {
                _selectedThirdLevelNode = value;
                OnPropertyChanged();
            }
        }

        public WidgetSettingsTriumphsView()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is PresentationNode paramNode)
            {
                // TODO: is this thread safe?
                SelectedTopLevelNode = paramNode.ParentNode;
                SelectedSecondLevelNode = paramNode;
                SkipSecondLevel = paramNode.SkipSecondLevel;

                if (SkipSecondLevel)
                {
                    SelectedThirdLevelNode = paramNode;
                }

                Log.Info("On navigated to, SkipSecondLevel: {v}", SkipSecondLevel);
            }

            notifier.Subscribe(this);
            UpdateViewModel();
        }

        public void HandleMessage(WidgetPropertyChanged message)
        {
            switch (message)
            {
                case WidgetPropertyChanged.Profile:
                case WidgetPropertyChanged.DefinitionsPath:
                    UpdateViewModel();
                    break;
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            notifier.Unsubscribe(this);
        }

        private void UpdateViewModel()
        {
            // TODO: handle first render better before user selected item
            // - list shouldnt flash so often

            if (!AppState.Data.DefinitionsLoaded) return;

            Log.Info("UpdateViewModel");

            UpdateSecondLevel();
            UpdateThirdLevel();
            UpdateTriumphsListingView();

            Log.Info($"SelectedTopLevelNode: {SelectedTopLevelNode?.Definition?.DisplayProperties?.Name}");
            Log.Info($"SelectedSecondLevelNode: {SelectedSecondLevelNode?.Definition?.DisplayProperties?.Name} Contains: {secondLevelNodes.Contains(SelectedSecondLevelNode)}");
            Log.Info($"SelectedThirdLevelNode: {SelectedThirdLevelNode?.Definition?.DisplayProperties?.Name} Contains: {thirdLevelNodes.Contains(SelectedThirdLevelNode)}");
        }

        private async void UpdateSecondLevel()
        {
            if (SkipSecondLevel)
            {
                SelectedSecondLevelNode = SelectedTopLevelNode;
                return;
            }

            // Clear items. Maybe we shouldnt do this?
            secondLevelNodes.Clear();

            // First, set up the list of items
            var topLevelDef = SelectedTopLevelNode.Definition;

            foreach (var child in topLevelDef.Children.PresentationNodes)
            {
                var childNode = new PresentationNode
                {
                    PresentationNodeHash = child.PresentationNodeHash,
                    Definition = await Definitions.GetPresentationNode(child.PresentationNodeHash)
                };

                secondLevelNodes.Add(childNode);

                if ((SelectedSecondLevelNode?.PresentationNodeHash ?? 0) == child.PresentationNodeHash)
                {
                    SelectedSecondLevelNode = childNode;
                }
            }

            // Second, select a current item
            if (SelectedSecondLevelNode == null)
            {
                SelectedSecondLevelNode = secondLevelNodes[0];
            } else if (!secondLevelNodes.Contains(SelectedSecondLevelNode))
            {
                SelectedSecondLevelNode =
                    secondLevelNodes.FirstOrDefault(v =>
                        v.PresentationNodeHash == SelectedSecondLevelNode.PresentationNodeHash) ??
                    secondLevelNodes.First();
            }
        }

        private async void UpdateThirdLevel()
        {
            // TODO: Figure out if we can not clear this out
            thirdLevelNodes.Clear();
            Log.Info("UpdateThirdLevel, SkipSecondLevel: {v}", SkipSecondLevel);

            if (SelectedSecondLevelNode == null)
            {
                return;
            }

            var children = SkipSecondLevel
                ? SelectedTopLevelNode.Definition.Children
                : SelectedSecondLevelNode.Definition.Children;

            foreach (var child in children.PresentationNodes)
            {
                var childNode = new PresentationNode
                {
                    PresentationNodeHash = child.PresentationNodeHash,
                    Definition = await Definitions.GetPresentationNode(child.PresentationNodeHash)
                };
                
                thirdLevelNodes.Add(childNode);
            }

            // Then update the selected item
            if (SelectedThirdLevelNode == null)
            {
                SelectedThirdLevelNode = thirdLevelNodes[0];
            } else if (!thirdLevelNodes.Contains(SelectedThirdLevelNode))
            {
                SelectedThirdLevelNode = thirdLevelNodes.FirstOrDefault(v =>
                                             v.PresentationNodeHash == SelectedThirdLevelNode.PresentationNodeHash) ??
                                         thirdLevelNodes.First();
            }
        }

        private void UpdateTriumphsListingView()
        {
            TriumphsFrame.Navigate(typeof(TriumphsListing), SelectedThirdLevelNode.PresentationNodeHash);
        }

        private void OnSecondLevelNodeClicked(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is PresentationNode selectedNode)
            {
                SelectedSecondLevelNode = selectedNode;

                // Clear out the selected hash to the default can be handled
                SelectedThirdLevelNode = default;

                UpdateThirdLevel();
                UpdateTriumphsListingView();
            }
        }

        private void OnThirdLevelNodeClicked(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is PresentationNode selectedNode)
            {
                SelectedThirdLevelNode = selectedNode;
                UpdateTriumphsListingView();
            }
        }

        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}