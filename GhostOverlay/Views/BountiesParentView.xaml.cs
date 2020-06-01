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

        private readonly ObservableCollection<ItemTrait> ItemTraits = new ObservableCollection<ItemTrait>();

        private readonly WidgetStateChangeNotifier notifier = new WidgetStateChangeNotifier();

        private ItemTrait _selectedTrait;

        public BountiesParentView()
        {
            InitializeComponent();
        }

        private ItemTrait SelectedTrait
        {
            get => _selectedTrait;
            set
            {
                _selectedTrait = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

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

            ItemTraits.Clear();

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
                ItemTraits.Add(trait);
            }

            // If the user has changed character, we'll have a new list of traits for the new character
            // however SelectedTrait will still be from the old character, so we go through and find
            // a new matching trait to set as active
            if (SelectedTrait != null && !ItemTraits.Contains(SelectedTrait))
            {
                var newSelectedTrait = ItemTraits.FirstOrDefault(v => v.TraitId == SelectedTrait.TraitId) ??
                                ItemTraits.First();

                if (newSelectedTrait.TraitId != SelectedTrait.TraitId)
                {
                    BountiesFrame.Navigate(typeof(WidgetSettingsBountiesView), newSelectedTrait);
                }

                SelectedTrait = newSelectedTrait;
            }
            else if (SelectedTrait == null || !ItemTraits.Contains(SelectedTrait))
            {
                SelectedTrait = ItemTraits.First();
                BountiesFrame.Navigate(typeof(WidgetSettingsBountiesView), SelectedTrait);
            }
        }

        private void OnTraitClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is ItemTrait trait)
            {
                BountiesFrame.Navigate(typeof(WidgetSettingsBountiesView), trait);
                SelectedTrait = trait;
            }
        }

        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}