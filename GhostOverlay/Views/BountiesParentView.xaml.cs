using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using GhostOverlay.Models;


// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace GhostOverlay.Views
{
    /// <summary>
    ///     An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class BountiesParentView : Page, ISubscriber<WidgetPropertyChanged>, INotifyPropertyChanged
    {
#pragma warning disable 67
        public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore 67

        private static readonly Logger Log = new Logger("BountiesParentView");
        private static readonly string[] IgnoreTraits = {"item_type.bounty"};

        private static readonly List<string> TraitOrder = new List<string>
        {
            "inventory_filtering.bounty",
            "inventory_filtering.quest",

            "quest.new_light",
            "quest.current_release",
            "quest.seasonal",
            "quest.playlists",
            "quest.exotic",
            "quest.past"
        };

        private readonly ObservableCollection<ItemTrait> itemTraits = new ObservableCollection<ItemTrait>();

        private readonly WidgetStateChangeNotifier notifier = new WidgetStateChangeNotifier();

        private ItemTrait SelectedTrait { get; set; }

        public BountiesParentView()
        {
            InitializeComponent();
        }

        public void HandleMessage(WidgetPropertyChanged message)
        {
            switch (message)
            {
                case WidgetPropertyChanged.Profile:
                case WidgetPropertyChanged.DefinitionsPath:
                case WidgetPropertyChanged.ActiveCharacter:
                    UpdateViewModel();
                    break;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            notifier.Subscribe(this);
            UpdateViewModel();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            notifier.Unsubscribe(this);
        }

        private async void UpdateViewModel()
        {
            var profile = AppState.Data.Profile;

            if (profile?.CharacterInventories?.Data == null || !AppState.Data.DefinitionsLoaded ||
                AppState.Data.ActiveCharacter == null) return;

            var bountiesForCharacter = await Item.ItemsFromProfile(profile, AppState.Data.ActiveCharacter);

            itemTraits.Clear();

            var traits = bountiesForCharacter
                .SelectMany(v => v.Definition?.TraitIds ?? new List<string>())
                .Distinct()
                .OrderBy(v =>
                {
                    var index = TraitOrder.IndexOf(v);
                    return index == -1 ? 999 : index;
                })
                .ToList();

            foreach (var traitId in traits)
            {
                if (IgnoreTraits.Contains(traitId)) continue;

                var trait = new ItemTrait {TraitId = traitId};
                await trait.PopulateDefinition();
                itemTraits.Add(trait);
            }

            // If the user has changed character, we'll have a new list of traits for the new character
            // however SelectedTrait will still be from the old character, so we go through and find
            // a new matching trait to set as active
            if (SelectedTrait != null && !itemTraits.Contains(SelectedTrait))
            {
                var newSelectedTrait = itemTraits.FirstOrDefault(v => v.TraitId == SelectedTrait.TraitId) ??
                                itemTraits.First();

                if (newSelectedTrait.TraitId != SelectedTrait.TraitId)
                {
                    BountiesFrame.Navigate(typeof(WidgetSettingsBountiesView), newSelectedTrait);
                }

                SelectedTrait = newSelectedTrait;
            }
            else if (SelectedTrait == null || !itemTraits.Contains(SelectedTrait))
            {
                SelectedTrait = itemTraits.First();
                BountiesFrame.Navigate(typeof(WidgetSettingsBountiesView), SelectedTrait);
            }
        }

        private void OnTraitClick(object sender, ItemClickEventArgs e)
        {
            if (!(e.ClickedItem is ItemTrait trait)) return;

            BountiesFrame.Navigate(typeof(WidgetSettingsBountiesView), trait);
            SelectedTrait = trait;
        }
    }
}