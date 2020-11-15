using System.Collections.Generic;
using System.Diagnostics;
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

        private const long RootTriumphsNodeHash = 1866538467;
        private const long RootSealsNodeHash = 616318467;
        private const long RootCatalystsNodeHash = 1984921914;
        private const long RootLoreNodeHash = 4077680549;

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
                case WidgetPropertyChanged.Profile:
                    UpdateViewModel();
                    break;
            }
        }

        private async void UpdateViewModel()
        {
            var nodes = new List<PresentationNode>();
            if (!AppState.Data.DefinitionsLoaded) return;

            var rootNodeDef = await Definitions.GetPresentationNode(RootTriumphsNodeHash);

            async Task<PresentationNode> OnEachSecondLevelNode(DestinyPresentationNodeChildEntry secondLevelChild, PresentationNode topLevelNode, bool skipSecondLevel = false)
            {
                var secondLevelNode = await PresentationNode.FromHash(secondLevelChild.PresentationNodeHash,
                    AppState.Data.Profile, topLevelNode);

                secondLevelNode.SkipSecondLevel = skipSecondLevel;
                nodes.Add(secondLevelNode);
                return secondLevelNode;
            }

            if (rootNodeDef != null)
            {
                foreach (var topLevelChild in rootNodeDef.Children.PresentationNodes)
                {
                    var topLevelNode = new PresentationNode
                    {
                        PresentationNodeHash = topLevelChild.PresentationNodeHash,
                        Definition = await Definitions.GetPresentationNode(topLevelChild.PresentationNodeHash)

                    };

                    if (topLevelNode.Definition == null)
                    {
                        continue;
                    }

                    foreach (var secondLevelChild in topLevelNode.Definition.Children.PresentationNodes)
                    {
                        await OnEachSecondLevelNode(secondLevelChild, topLevelNode);
                    }
                }
            }

            //
            // Catalysts
            //
            var catalystsNode = new PresentationNode
            {
                PresentationNodeHash = RootCatalystsNodeHash,
                Definition = await Definitions.GetPresentationNode(RootCatalystsNodeHash)
            };

            if (catalystsNode.Definition != null)
            {
                foreach (var secondLevelChild in catalystsNode.Definition.Children.PresentationNodes)
                {
                    var node = await OnEachSecondLevelNode(secondLevelChild, catalystsNode, true);
                    node.Definition.DisplayProperties.HasIcon = true;
                    node.Definition.DisplayProperties.Icon = catalystsNode.Definition.DisplayProperties.Icon;
                }
            }

            //
            // Lore
            //
            var loreNode = new PresentationNode
            {
                PresentationNodeHash = RootLoreNodeHash,
                Definition = await Definitions.GetPresentationNode(RootLoreNodeHash)
            };

            if (loreNode.Definition != null)
            {
                foreach (var secondLevelChild in loreNode.Definition.Children.PresentationNodes)
                {
                    await OnEachSecondLevelNode(secondLevelChild, loreNode);
                }
            }

            //
            // Seals
            //
            var sealsNode = new PresentationNode
            {
                PresentationNodeHash = RootSealsNodeHash,
                Definition = await Definitions.GetPresentationNode(RootSealsNodeHash)
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
