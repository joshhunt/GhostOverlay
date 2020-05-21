using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using GhostOverlay.Models;
using GhostOverlay.Views;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace GhostOverlay
{
    /// <summary>
    ///     An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class WidgetSettingsTriumphsView : Page, ISubscriber<WidgetPropertyChanged>, INotifyPropertyChanged
    {
        private readonly MyEventAggregator eventAggregator = new MyEventAggregator();
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly RangeObservableCollection<PresentationNode> secondLevelNodes =
            new RangeObservableCollection<PresentationNode>();

        private readonly RangeObservableCollection<PresentationNode> thirdLevelNodes =
            new RangeObservableCollection<PresentationNode>();

        private bool viewIsUpdating;

        // cleaned up stuff
        private PresentationNode _selectedTopLevelNode;
        private PresentationNode _selectedSecondLevelNode;
        private PresentationNode _selectedThirdLevelNode;

        private PresentationNode SelectedTopLevelNode
        {
            get { return _selectedTopLevelNode; }
            set
            {
                _selectedTopLevelNode = value;
                OnPropertyChanged();
            }
        }

        private PresentationNode SelectedSecondLevelNode
        {
            get { return _selectedSecondLevelNode; }
            set
            {
                _selectedSecondLevelNode = value;
                OnPropertyChanged();
            }
        }

        private PresentationNode SelectedThirdLevelNode
        {
            get { return _selectedThirdLevelNode; }
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
                //topPresentationHash = thisPresentationNode.ParentNode.PresentationNodeHash;
                //secondLevelPresentationNodeHash = thisPresentationNode.PresentationNodeHash;

                // TODO: is this thread safe?
                SelectedTopLevelNode = paramNode.ParentNode;
                SelectedSecondLevelNode = paramNode;
            }

            eventAggregator.Subscribe(this);
            UpdateViewModel();
        }

        public void HandleMessage(WidgetPropertyChanged message)
        {
            Debug.WriteLine($"HandleMessage in triumphs view {message}");
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

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            Debug.WriteLine("TriumphsView OnNavigatingFrom");
            base.OnNavigatingFrom(e);
            eventAggregator.Unsubscribe(this);
        }

        private void UpdateViewModel()
        {
            // TODO: handle first render better before user selected item
            // - list shouldnt flash so often

            viewIsUpdating = true;

            UpdateSecondLevel();
            UpdateThirdLevel();
            UpdateTriumphsListingView();

            viewIsUpdating = false;
        }

        private void UpdateSelection()
        {
            viewIsUpdating = true;
            viewIsUpdating = false;
        }

        private async void UpdateSecondLevel()
        {
            // Clear items. Maybe we shouldnt do this?
            secondLevelNodes.Clear();

            // First, set up the list of items
            var topLevelDef = SelectedTopLevelNode.Definition;

            foreach (var child in topLevelDef.Children.PresentationNodes)
            {
                var childNode = new PresentationNode
                {
                    PresentationNodeHash = child.PresentationNodeHash,
                    Definition = await Definitions.GetPresentationNode(Convert.ToUInt32(child.PresentationNodeHash))
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
                Debug.WriteLine("selectedSecondLevelNode == null - I don't think this should happen?");
                SelectedSecondLevelNode = secondLevelNodes[0];
            }
        }

        private async void UpdateThirdLevel()
        {
            viewIsUpdating = true;

            // TODO: Figure out if we can not clear this out
            thirdLevelNodes.Clear();

            if (SelectedSecondLevelNode == null)
            {
                Debug.WriteLine("In UpdateThirdLevel, secondLevelNode really shouldnt be null");
                return;
            }

            foreach (var child in SelectedSecondLevelNode.Definition.Children.PresentationNodes)
            {
                var childNode = new PresentationNode
                {
                    PresentationNodeHash = child.PresentationNodeHash,
                    Definition = await Definitions.GetPresentationNode(Convert.ToUInt32(child.PresentationNodeHash))
                };
                
                thirdLevelNodes.Add(childNode);
            }

            // Then update the selected item
            if (SelectedThirdLevelNode == null)
            {
                SelectedThirdLevelNode = thirdLevelNodes[0];
            }

            viewIsUpdating = false;
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