using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using GhostSharper.Models;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace GhostOverlay.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsRootTriumphsView : Page, ISubscriber<WidgetPropertyChanged>
    {
        private readonly WidgetStateChangeNotifier eventAggregator = new WidgetStateChangeNotifier();
        private readonly long rootTriumphsNodeHash = 1024788583;
        private readonly long rootSealsNodeHash = 1652422747;
        private Frame parentFrame;

        public SettingsRootTriumphsView()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is Frame frame)
            {
                parentFrame = frame;
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
            switch (message) 
            {
                case WidgetPropertyChanged.DefinitionsPath:
                    UpdateViewModel();
                    break;
            }
        }

        private async void UpdateViewModel()
        {
            var nodes = new List<PresentationNode>();
            if (!AppState.Data.DefinitionsLoaded) return;

            var rootNode = await Definitions.GetPresentationNode(rootTriumphsNodeHash);

            async Task OnEachSecondLevelNode(DestinyPresentationNodeChildEntry secondLevelChild, PresentationNode topLevelNode, bool skipSecondLevel = false)
            {
                var secondLevelNode = await PresentationNode.FromHash(secondLevelChild.PresentationNodeHash,
                    AppState.Data.Profile, topLevelNode);
                secondLevelNode.SkipSecondLevel = skipSecondLevel;

                nodes.Add(secondLevelNode);
            }

            foreach (var topLevelChild in rootNode.Children.PresentationNodes)
            {
                var topLevelNode = new PresentationNode
                {
                    PresentationNodeHash = topLevelChild.PresentationNodeHash,
                    Definition =
                        await Definitions.GetPresentationNode(topLevelChild.PresentationNodeHash)
                };

                foreach (var secondLevelChild in topLevelNode.Definition.Children.PresentationNodes)
                {
                    await OnEachSecondLevelNode(secondLevelChild, topLevelNode);
                }
            }

            var sealsNode = new PresentationNode
            {
                PresentationNodeHash = rootSealsNodeHash,
                Definition = await Definitions.GetPresentationNode(rootSealsNodeHash)
            };

            if (sealsNode.Definition != null)
            {
                foreach (var secondLevelChild in sealsNode.Definition.Children.PresentationNodes)
                {
                    await OnEachSecondLevelNode(secondLevelChild, sealsNode, true);
                }
            }

            NodesCollection.Source =
                from t in nodes
                group t by t.ParentNode
                into g
                select g;
        }

        private void OnNodeClicked(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is PresentationNode selectedNode)
            {
                parentFrame.Navigate(typeof(WidgetSettingsTriumphsView), selectedNode);
            }
        }
    }
}
