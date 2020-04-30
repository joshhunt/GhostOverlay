using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using BungieNetApi.Model;
using GhostOverlay.Models;
using Microsoft.Gaming.XboxGameBar;
using Microsoft.Toolkit.Uwp.UI.Controls.TextToolbarSymbols;
using Microsoft.Toolkit.Uwp.UI.Extensions;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace GhostOverlay
{
    public sealed partial class WidgetMainView : Page, ISubscriber<PropertyChanged>
    {
        private enum VisualState
        {
            None,
            Empty,
            InitialProfileLoad,
            ProfileError,
            DefinitionsLoading
        }

        public TimerViewModel updateTimer;
        public ObservableCollection<Item> TrackedBounties = new ObservableCollection<Item>();
        private XboxGameBarWidget widget;
        private readonly MyEventAggregator eventAggregator = new MyEventAggregator();

        public WidgetMainView()
        {
            InitializeComponent();
            eventAggregator.Subscribe(this);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            widget = e.Parameter as XboxGameBarWidget;
            updateTimer = new TimerViewModel();

            if (widget != null)
            {
                widget.MaxWindowSize = new Size(1500, 1500);
                widget.MinWindowSize = new Size(200, 100);
                widget.HorizontalResizeSupported = true;
                widget.VerticalResizeSupported = true;
                widget.SettingsSupported = true;
                widget.SettingsClicked += Widget_SettingsClicked;
                widget.RequestedThemeChanged += Widget_RequestedThemeChanged;
                Widget_RequestedThemeChanged(widget, null);
            }

            SetVisualState(VisualState.InitialProfileLoad);

            AppState.WidgetData.ScheduleProfileUpdates();
            UpdateFromProfile();
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            eventAggregator.Subscribe(this);
            AppState.WidgetData.UnscheduleProfileUpdates();
        }

        public void HandleMessage(PropertyChanged message)
        {
            switch (message)
            {
                case PropertyChanged.Profile:
                case PropertyChanged.DefinitionsPath:
                case PropertyChanged.TrackedBounties:
                    UpdateFromProfile();
                    break;
            }
        }

        private void SetVisualState(VisualState state)
        {
            EmptyState.Visibility = Visibility.Collapsed;
            InitialProfileLoadState.Visibility = Visibility.Collapsed;
            ProfileErrorState.Visibility = Visibility.Collapsed;
            DefinitionsLoadingState.Visibility = Visibility.Collapsed;

            switch (state)
            {
                case VisualState.None:
                    break;

                case VisualState.Empty:
                    EmptyState.Visibility = Visibility.Visible;
                    break;

                case VisualState.InitialProfileLoad:
                    InitialProfileLoadState.Visibility = Visibility.Visible;
                    break;

                case VisualState.ProfileError:
                    ProfileErrorState.Visibility = Visibility.Visible;
                    break;

                case VisualState.DefinitionsLoading:
                    DefinitionsLoadingState.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void UpdateFromProfile()
        {
            var profile = AppState.WidgetData.Profile;

            if (profile?.CharacterInventories?.Data == null)
            {
                if (profile?.Profile?.Data?.UserInfo?.MembershipId != null)
                    SetVisualState(VisualState.ProfileError);

                return;
            }

            if (!AppState.WidgetData.DefinitionsLoaded)
            {
                SetVisualState(VisualState.DefinitionsLoading);
                return;
            }

            SetVisualState(VisualState.None);
            UpdateTracked();
        }

        private async void UpdateTracked()
        {
            var tracked = new List<ITrackable>();
            var profile = AppState.WidgetData.Profile;
            var characters = new Dictionary<long, Character>();
            var trackedEntriesToCleanup = new List<TrackedEntry>();
            
            foreach (var trackedEntry in AppState.WidgetData.TrackedEntries)
            {
                ITrackable trackable = default;

                switch (trackedEntry.Type)
                {
                    case TrackedEntryType.Record:
                        trackable = await TriumphFromTrackedEntry(trackedEntry, profile);
                        break;

                    case TrackedEntryType.Item:
                        trackable = ItemFromTrackedEntry(trackedEntry, profile, characters);
                        break;
                }

                if (trackable?.Objectives != null && trackable.Objectives.Count > 0)
                {
                    tracked.Add(trackable);
                }
                else
                {
                    Debug.WriteLine($"Tracked entry no longer exists {trackedEntry}");
                    trackedEntriesToCleanup.Add(trackedEntry);
                }
            }

            AppState.WidgetData.TrackedEntries.RemoveAll(trackedEntriesToCleanup.Contains);

            var groupedBounties =
                from t in tracked
                orderby t.IsCompleted
                group t by t.GroupByKey
                into g
                select g;

            SetVisualState(tracked.Count == 0 ? VisualState.Empty : VisualState.None);

            TrackedBountiesCollection.Source = groupedBounties;
        }

        private Item ItemFromTrackedEntry(TrackedEntry entry, DestinyResponsesDestinyProfileResponse profile, Dictionary<long, Character>  characters)
        {
            if (profile == null) return default;

            var itemInstanceId = entry.InstanceId.ToString();
            var characterId = entry.OwnerId.ToString();
            var hash = entry.Hash.ToString();

            var rawObjectives = new List<DestinyQuestsDestinyObjectiveProgress>();

            if (profile.ItemComponents.Objectives.Data.ContainsKey(itemInstanceId))
            {
                rawObjectives.AddRange(profile.ItemComponents.Objectives.Data[itemInstanceId].Objectives);
            }

            if (profile.CharacterUninstancedItemComponents.ContainsKey(characterId))
            {
                var uninstancedObjectivesData =
                    profile.CharacterUninstancedItemComponents[characterId].Objectives.Data;
                if (uninstancedObjectivesData.ContainsKey(hash))
                {
                    rawObjectives.AddRange(uninstancedObjectivesData[hash].Objectives);
                }
            }

            var objectives = rawObjectives.Select(v =>
            {
                var obj = new Objective { Progress = v };
                obj.PopulateDefinition();
                return obj;
            }).ToList();

            if (!characters.ContainsKey(entry.OwnerId))
            {
                characters[entry.OwnerId] = new Character { CharacterComponent = profile.Characters.Data[characterId] };
                characters[entry.OwnerId].PopulateDefinition();
            }

            var bounty = new Item
            {
                ItemHash = entry.Hash,
                ItemInstanceId = entry.InstanceId,
                Objectives = objectives,
                OwnerCharacter = characters[entry.OwnerId],
                TrackedEntry = entry
            };

            bounty.PopulateDefinition();

            return bounty;
        }

        private async Task<Triumph> TriumphFromTrackedEntry(TrackedEntry entry, DestinyResponsesDestinyProfileResponse profile)
        {
            if (profile == null) return default;

            var triumph = new Triumph {
                Hash = entry.Hash,
                TrackedEntry = entry
            };
            await triumph.PopulateDefinition();

            var isCharacterRecord = triumph.Definition.Scope == 1;
            var record = new DestinyComponentsRecordsDestinyRecordComponent();
            var characterIds = profile?.Profile?.Data?.CharacterIds ?? new List<long>();

            if (isCharacterRecord)
                foreach (var characterId in characterIds)
                {
                    // TODO: we should probably return the most complete one, rather than the first we find?
                    var recordsForCharacter = profile.CharacterRecords.Data[characterId.ToString()];
                    record = recordsForCharacter.Records[triumph.Hash.ToString()];

                    if (record != null) break;
                }
            else
                record = profile.ProfileRecords.Data.Records[triumph.Hash.ToString()];

            triumph.Record = record;

            var objectives = (record?.IntervalObjectives?.Count ?? 0) > 0
                ? record.IntervalObjectives
                : record?.Objectives;

            triumph.Objectives = objectives?.ConvertAll(v =>
            {
                var obj = new Objective { Progress = v };
                obj.PopulateDefinition();
                return obj;
            });

            return triumph;
        }

        private void RemoveTrackedEntry(TrackedEntry entry)
        {
            var copyOf = AppState.WidgetData.TrackedEntries.ToList();
            copyOf.RemoveAll(v => v == entry);
            AppState.WidgetData.TrackedEntries = copyOf;
        }

        private async void Widget_SettingsClicked(XboxGameBarWidget sender, object args)
        {
            await widget.ActivateSettingsAsync();
        }

        private async void SettingsButton_OnClick(object sender, RoutedEventArgs e)
        {
            await widget.ActivateSettingsAsync();
        }

        private async void Widget_RequestedThemeChanged(XboxGameBarWidget sender, object args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () =>
                {
                    Background = widget.RequestedTheme == ElementTheme.Dark
                        ? new SolidColorBrush(Color.FromArgb(255, 0, 0, 0))
                        : new SolidColorBrush(Color.FromArgb(255, 76, 76, 76));
                });
        }

        private void UntrackItem_OnClick(object sender, RoutedEventArgs e)
        {
            var button = sender as MenuFlyoutItem;

            if (button.Tag is TrackedEntry trackedEntry)
            {
                RemoveTrackedEntry(trackedEntry);
            }
        }
    }
}