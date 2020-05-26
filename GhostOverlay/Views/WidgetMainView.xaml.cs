using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
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
    public sealed partial class WidgetMainView : Page, ISubscriber<WidgetPropertyChanged>, INotifyPropertyChanged
    {
        private enum VisualState
        {
            None,
            Empty,
            InitialProfileLoad,
            ProfileError,
            DefinitionsLoading
        }

        private XboxGameBarWidget widget;
        private readonly WidgetStateChangeNotifier eventAggregator = new WidgetStateChangeNotifier();
        private readonly List<ITrackable> Tracked = new List<ITrackable>();

        // Timer stuff
        private DispatcherTimer timer;
        public event PropertyChangedEventHandler PropertyChanged;

        public string SinceProfileUpdate
        {
            get
            {
                if (AppState.Data.ProfileUpdatedTime == DateTime.MinValue)
                {
                    return "-";
                }

                var span = DateTime.Now - AppState.Data.ProfileUpdatedTime;

                if (span.TotalSeconds > 60)
                    return $"{span.Minutes}m {span.Seconds}s";

                return $"{span.Seconds}s";
            }
        }

        public WidgetMainView()
        {
            InitializeComponent();

            this.DataContext = this;
        }

        private void timer_Tick(object sender, object e)
        {
            RaisePropertyChanged("SinceProfileUpdate");
        }

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            widget = e.Parameter as XboxGameBarWidget;
            
            if (widget != null)
            {
                widget.MaxWindowSize = new Size(1500, 1500);
                widget.MinWindowSize = new Size(200, 100);
                widget.HorizontalResizeSupported = true;
                widget.VerticalResizeSupported = true;
                widget.SettingsSupported = true;
                widget.SettingsClicked += Widget_SettingsClicked;
            }

            SetVisualState(VisualState.InitialProfileLoad);

            eventAggregator.Subscribe(this);
            AppState.Data.ScheduleProfileUpdates();
            UpdateFromProfile();

            timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += timer_Tick;
            timer.Start();
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            eventAggregator.Unsubscribe(this);
            AppState.Data.UnscheduleProfileUpdates();
        }

        public void HandleMessage(WidgetPropertyChanged message)
        {
            //Log($"HandleMessage {message}");

            switch (message)
            {
                case WidgetPropertyChanged.Profile:
                case WidgetPropertyChanged.DefinitionsPath:
                case WidgetPropertyChanged.TrackedItems:
                    UpdateFromProfile();
                    break;

                case WidgetPropertyChanged.ProfileUpdating:
                    UpdateProfileUpdating();
                    break;

                case WidgetPropertyChanged.TokenData:
                    CheckAuth();
                    break;
            }
        }

        private void CheckAuth()
        {
            if (AppState.Data.TokenData == null || !AppState.Data.TokenData.IsValid())
            {
                // Navigate frame to the "not logged in" view
                if (this.Frame.CanGoBack && this.Frame.BackStack[this.Frame.BackStack.Count - 1].SourcePageType == typeof(WidgetNotAuthedView))
                {
                    this.Frame.GoBack();
                }
                else
                {
                    this.Frame.Navigate(typeof(WidgetNotAuthedView), widget);
                }
            }
        }

        private void UpdateProfileUpdating()
        {
            if (AppState.Data.ProfileIsUpdating)
            {
                ProfileUpdatingProgressRing.IsActive = true;
            }
            else
            {
                ProfileUpdatingProgressRing.IsActive = false;
            }
        }

        private void SetVisualState(VisualState state)
        {
            EmptyState.Visibility = Visibility.Collapsed;
            InitialProfileLoadState.Visibility = Visibility.Collapsed;
            ProfileErrorState.Visibility = Visibility.Collapsed;
            DefinitionsLoadingState.Visibility = Visibility.Collapsed;
            ProfileUpdatesPanel.Visibility = Visibility.Collapsed;

            switch (state)
            {
                case VisualState.None:
                    ProfileUpdatesPanel.Visibility = Visibility.Visible;
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
            var profile = AppState.Data.Profile;

            if (profile?.CharacterInventories?.Data == null)
            {
                if (profile?.Profile?.Data?.UserInfo?.MembershipId != null)
                    SetVisualState(VisualState.ProfileError);

                return;
            }

            if (!AppState.Data.DefinitionsLoaded)
            {
                SetVisualState(VisualState.DefinitionsLoading);
                return;
            }

            SetVisualState(VisualState.None);
            UpdateTracked();
        }

        private async void UpdateTracked()
        {
            var profile = AppState.Data.Profile;
            var characters = new Dictionary<long, Character>();
            var toCleanup = new List<TrackedEntry>();

            Tracked.Clear();

            foreach (var trackedEntry in AppState.Data.TrackedEntries)
            {
                ITrackable trackable = default;

                switch (trackedEntry.Type)
                {
                    case TrackedEntryType.Record:
                        trackable = await TriumphFromTrackedEntry(trackedEntry, profile);
                        break;

                    case TrackedEntryType.Item:
                        trackable = await ItemFromTrackedEntry(trackedEntry, profile, characters);
                        break;
                }

                if (trackable?.Objectives != null && trackable.Objectives.Count > 0)
                    Tracked.Add(trackable);
                else
                    toCleanup.Add(trackedEntry);
            }

            AppState.Data.TrackedEntries.RemoveAll(toCleanup.Contains);

            var groupedBounties =
                from t in Tracked
                orderby t.SortValue
                group t by t.GroupByKey
                into g
                select g;

            SetVisualState(Tracked.Count == 0 ? VisualState.Empty : VisualState.None);
            TrackedBountiesCollection.Source = groupedBounties;
        }

        private async Task<Item> ItemFromTrackedEntry(TrackedEntry entry, DestinyResponsesDestinyProfileResponse profile, Dictionary<long, Character>  characters)
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

            var objectives = new List<Objective>();
            foreach (var objectiveProgress in rawObjectives)
            {
                var obj = new Objective { Progress = objectiveProgress };
                await obj.PopulateDefinition();
                objectives.Add(obj);
            }

            if (!characters.ContainsKey(entry.OwnerId))
            {
                characters[entry.OwnerId] = new Character { CharacterComponent = profile.Characters.Data[characterId] };
                await characters[entry.OwnerId].PopulateDefinition();
            }

            var bounty = new Item
            {
                ItemHash = entry.Hash,
                ItemInstanceId = entry.InstanceId,
                Objectives = objectives,
                OwnerCharacter = characters[entry.OwnerId],
                TrackedEntry = entry
            };

            await bounty.PopulateDefinition();

            return bounty;
        }

        

        private async Task<Triumph> TriumphFromTrackedEntry(TrackedEntry entry, DestinyResponsesDestinyProfileResponse profile)
        {
            if (profile == null) return default;

            var triumph = new Triumph {
                Hash = entry.Hash,
                TrackedEntry = entry,
                Objectives = new List<Objective>()
            };
            await triumph.PopulateDefinition();

            triumph.Record = Triumph.FindRecordInProfile(triumph.Hash.ToString(), profile);

            if (triumph.Record == null) return default;

            var objectives = (triumph.Record?.IntervalObjectives?.Count ?? 0) > 0
                ? triumph.Record.IntervalObjectives
                : (triumph.Record?.Objectives ?? new List<DestinyQuestsDestinyObjectiveProgress>());

            foreach (var objectiveProgress in objectives)
            {
                var obj = new Objective { Progress = objectiveProgress };
                await obj.PopulateDefinition();
                triumph.Objectives.Add(obj);
            }

            return triumph;
        }

        private void RemoveTrackedEntry(TrackedEntry entry)
        {
            var copyOf = AppState.Data.TrackedEntries.ToList();
            copyOf.RemoveAll(v => v == entry);
            AppState.Data.TrackedEntries = copyOf;
        }

        private async void Widget_SettingsClicked(XboxGameBarWidget sender, object args)
        {
            Log("Tryna open settings");
            await widget.ActivateSettingsAsync();
        }

        private async void SettingsButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (widget != null)
            {
                // We're in Game Bar
                await widget.ActivateSettingsAsync();
                return;
            }

            // View has been launched outside of game bar.
            // Note: this code is non optimal
            AppWindow appWindow = await AppWindow.TryCreateAsync();
            Frame appWindowContentFrame = new Frame();
            appWindowContentFrame.Navigate(typeof(WidgetSettingsView));
            ElementCompositionPreview.SetAppWindowContent(appWindow, appWindowContentFrame);
            await appWindow.TryShowAsync();
        }

        private void UntrackItem_OnClick(object sender, RoutedEventArgs e)
        {
            var button = sender as MenuFlyoutItem;

            if (button.Tag is TrackedEntry trackedEntry)
            {
                RemoveTrackedEntry(trackedEntry);
            }
        }

        private void ClearAllItems_OnClick(object sender, RoutedEventArgs e)
        {
            AppState.Data.TrackedEntries = new List<TrackedEntry>();
        }

        private void Log(string message)
        {
            Debug.WriteLine($"[WidgetMainView] {message}");
        }
    }
}