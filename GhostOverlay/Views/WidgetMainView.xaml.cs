using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media.SpeechSynthesis;
using Windows.Storage;
using Windows.System;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using GhostOverlay.Models;
using GhostSharper.Models;
using Microsoft.Gaming.XboxGameBar;
using Microsoft.Toolkit.Collections;

namespace GhostOverlay
{
    enum VisualState
    {
        None,
        Empty,
        InitialProfileLoad,
        ProfileError,
        DefinitionsLoading
    }

    public sealed partial class WidgetMainView : Page, ISubscriber<WidgetPropertyChanged>, INotifyPropertyChanged
    {
#pragma warning disable 67
        public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore 67

        private static readonly Logger Log = new Logger("WidgetMainView");

        private XboxGameBarWidget widget;
        private readonly WidgetStateChangeNotifier eventAggregator = new WidgetStateChangeNotifier();
        private readonly ObservableGroupedCollection<TrackableOwner, ITrackable> TrackedSource = new ObservableGroupedCollection<TrackableOwner, ITrackable>();

        private bool IsBustingProfileRequests { get; set; }
        private bool ShowDevOptions { get; set; }
        private string ErrorMessage { get; set; }

        // Timer stuff
        private DispatcherTimer timer;

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
            OnPropertyChanged($"SinceProfileUpdate");
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

                widget.RequestedOpacityChanged += Widget_RequestedOpacityChanged;
                _ = SetWidgetOpacity();
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
            switch (message)
            {
                case WidgetPropertyChanged.Profile:
                case WidgetPropertyChanged.TrackedItems:
                case WidgetPropertyChanged.ShowDescriptions:
                case WidgetPropertyChanged.DefinitionsPath:
                    UpdateFromProfile();
                    break;

                case WidgetPropertyChanged.ProfileUpdating:
                    UpdateProfileUpdating();
                    break;

                case WidgetPropertyChanged.TokenData:
                    CheckAuth();
                    break;

                case WidgetPropertyChanged.BustProfileRequests:
                case WidgetPropertyChanged.ProfileError:
                case WidgetPropertyChanged.ShowDevOptions:
                    UpdateMiscViewItems();
                    break;

                // case WidgetPropertyChanged.DefinitionsPath:
                //     UpdateDefinitions();
                //     break;
            }
        }

        private async void UpdateDefinitions()
        {
            foreach (var observableGroup in TrackedSource)
            {
                foreach (var trackable in observableGroup)
                {
                    Debug.WriteLine("re populating definition");
                    switch (trackable)
                    {
                        case Item item:
                            await item.PopulateDefinition();
                            break;
                        case Triumph triumph:
                            await triumph.PopulateDefinition();
                            break;
                        case CrucibleMapTrackable crucibleMapTrackable:
                            await crucibleMapTrackable.PopulateDefinitions();
                            break;
                    }
                }
            }
        }

        private void UpdateMiscViewItems()
        {
            if (AppState.Data.BustProfileRequests.Value != IsBustingProfileRequests)
            {
                IsBustingProfileRequests = AppState.Data.BustProfileRequests.Value;
            }

            if (AppState.Data.ProfileError.Value != ErrorMessage)
            {
                ErrorMessage = AppState.Data.ProfileError.Value;
                ErrorOverlay.Visibility = !string.IsNullOrEmpty(ErrorMessage) ? Visibility.Visible : Visibility.Collapsed;
            }

            if (AppState.Data.ShowDevOptions.Value != ShowDevOptions)
            {
                ShowDevOptions = AppState.Data.ShowDevOptions.Value;
            }
        }

        private void CheckAuth()
        {
            if (AppState.Data.TokenData == null || !AppState.Data.TokenData.IsValid())
            {
                // Navigate frame to the "not logged in" view
                if (Frame.CanGoBack && Frame.BackStack[Frame.BackStack.Count - 1].SourcePageType == typeof(WidgetNotAuthedView))
                {
                    Frame.GoBack();
                }
                else
                {
                    Frame.Navigate(typeof(WidgetNotAuthedView), widget);
                }
            }
        }

        private void UpdateProfileUpdating()
        {
            ProfileUpdatingProgressRing.IsActive = AppState.Data.ProfileIsUpdating;
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
            Log.Info("UpdateFromProfile");

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
            Log.Info("UpdateTracked");

            var profile = AppState.Data.Profile;
            var characters = new Dictionary<long, TrackableOwner>();

            var tracked = new List<ITrackable>();
            var toCleanup = new List<TrackedEntry>();

            foreach (var trackedEntry in AppState.Data.TrackedEntries.ToList())
            {
                ITrackable trackable = default;

                switch (trackedEntry.Type)
                {
                    case TrackedEntryType.Record:
                    case TrackedEntryType.SeasonalChallenge:
                        trackable = await TriumphFromTrackedEntry(trackedEntry, profile);
                        break;

                    case TrackedEntryType.Item:
                        trackable = await ItemFromTrackedEntry(trackedEntry, profile, characters);
                        break;

                    case TrackedEntryType.DynamicTrackable:
                        Log.Info("  found a DynamicTrackable");
                        trackable = await DynamicTrackableFromTrackedEntry(trackedEntry, profile);
                        break;
                }

                if (trackable != null)
                    tracked.Add(trackable);
                else
                    toCleanup.Add(trackedEntry);
            }

            TrackedSource.SetTrackables(tracked);

            AppState.Data.TrackedEntries.RemoveAll(toCleanup.Contains);
            AppState.SaveTrackedEntries(AppState.Data.TrackedEntries);

            SetVisualState(tracked.Count == 0 ? VisualState.Empty : VisualState.None);
        }

        private async Task<TrackedEntry> UpgradeQuestStepTrackedEntry(TrackedEntry oldTrackedEntry, DestinyInventoryItemDefinition itemDefinition, DestinyProfileResponse profile)
        {
            var questStepsHashes = itemDefinition?.SetData?.ItemList?.Select(v => v.ItemHash).ToList() ??
                                   new List<uint>();
            var questlineItemHash = itemDefinition?.Objectives?.QuestlineItemHash ?? 0;

            if (questlineItemHash != 0)
            {
                var questLineItem = await Definitions.GetInventoryItem(questlineItemHash);
                foreach (var questStepHash in questLineItem?.SetData?.ItemList?.Select(v => v.ItemHash) ?? new List<uint>())
                {
                    if (!questStepsHashes.Contains(questStepHash))
                    {
                        questStepsHashes.Add(questStepHash);
                    }
                }
            }

            Log.Debug("UpgradeQuestStepTrackedEntry, tracked entry {trackedEntry}, found quest step hashes {questStepsHashes}", oldTrackedEntry, questStepsHashes);

            var charactersInventory = profile?.CharacterInventories?.Data;
            var characterId = oldTrackedEntry.OwnerId.ToString();

            if (charactersInventory == null || !charactersInventory.ContainsKey(characterId))
            {
                Log.Info("UpgradeQuestStepTrackedEntry, could not find inventory for character ID {characterID}", characterId);
                return default;
            }
            
            return charactersInventory[characterId].Items
                .Where(itemComponent => questStepsHashes.Contains(itemComponent.ItemHash))
                .Select(itemComponent => TrackedEntry.FromInventoryItemComponent(itemComponent, oldTrackedEntry.OwnerId))
                .FirstOrDefault();
        }

        private async Task<DynamicTrackable> DynamicTrackableFromTrackedEntry(TrackedEntry trackedEntry, DestinyProfileResponse profile)
        {
            switch (trackedEntry.DynamicTrackableType)
            {
                case DynamicTrackableType.CrucibleMap:
                    Log.Info("  Dynamic trackable is a DynamicTrackableType.CrucibleMap");
                    var crucibleMapTracker = await CrucibleMapTrackable.CreateFromProfile(profile, trackedEntry);
                    Log.Info("  crucibleMapTracker: {crucibleMapTracker}", crucibleMapTracker);
                    return crucibleMapTracker;
            }

            return default;
        }

        private async Task<Item> ItemFromTrackedEntry(TrackedEntry entry, DestinyProfileResponse profile, Dictionary<long, TrackableOwner> characters)
        {
            if (profile == null || entry == null) return default;

            var itemInstanceId = entry.InstanceId.ToString();
            var characterId = entry.OwnerId.ToString();
            var hash = entry.Hash.ToString();
                

            if (!profile.Characters.Data.ContainsKey(characterId))
            {
                Log.Warn("Tracked entry is for character ID {characterId} which is not from this profile.", characterId);
                return default;
            }

            var rawObjectives = new List<DestinyObjectiveProgress>();

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
                characters[entry.OwnerId] = new TrackableOwner { CharacterComponent = profile.Characters.Data[characterId] };
                await characters[entry.OwnerId].PopulateDefinition();
            }

            var bounty = new Item
            {
                ItemHash = entry.Hash,
                ItemInstanceId = entry.InstanceId,
                Objectives = objectives,
                Owner = characters[entry.OwnerId],
                TrackedEntry = entry
            };

            await bounty.PopulateDefinition();

            if (bounty?.Objectives != null && bounty.Objectives.Count > 0)
            {
                // We found the pursuit fine, return it early.
                return bounty;
            }

            // We were unable to find the pursuit, so maybe we can find further quest steps?
            var newTrackedEntry = await UpgradeQuestStepTrackedEntry(entry, bounty.Definition, profile);
            var newTrackedItem = await ItemFromTrackedEntry(newTrackedEntry, profile, characters);

            if (newTrackedEntry != null)
            {
                var oldTrackedEntryIndex = AppState.Data.TrackedEntries.FindIndex(item => item.Equals(entry));
                if (oldTrackedEntryIndex > -1)
                {
                    AppState.Data.TrackedEntries[oldTrackedEntryIndex] = newTrackedEntry;
                    Log.Info("Replacing tracked entry {entry} at index {index} with new entry {newEntry}", entry, oldTrackedEntryIndex, newTrackedEntry);
                    AppState.SaveTrackedEntries(AppState.Data.TrackedEntries); // we modified in place, so just serialise and save
                }
            }

            return newTrackedItem?.Objectives?.Count == 0
                ? default
                : newTrackedItem;
        }


        private static async Task<Triumph> TriumphFromTrackedEntry(TrackedEntry entry, DestinyProfileResponse profile)
        {
            if (profile == null) return default;

            var triumph = new Triumph {
                Hash = entry.Hash,
                TrackedEntry = entry,
                Objectives = new List<Objective>(),
                Record = Triumph.FindRecordInProfileOrDefault(entry.Hash.ToString(), profile)
            };
            await triumph.PopulateDefinition();

            if (triumph.Record == null) return default;

            foreach (var objectiveProgress in triumph.ObjectiveProgresses)
            {
                if (objectiveProgress == null) continue;

                var obj = new Objective { Progress = objectiveProgress };
                await obj.PopulateDefinition();
                triumph.Objectives.Add(obj);
            }

            return triumph;
        }

        private void RemoveTrackedEntry(TrackedEntry entry)
        {
            var copyOf = AppState.Data.TrackedEntries.ToList();
            copyOf.RemoveAll(v => v.Equals(entry));
            AppState.Data.TrackedEntries = copyOf;
        }

        private async void Widget_SettingsClicked(XboxGameBarWidget sender, object args)
        {
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

        private async void Widget_RequestedOpacityChanged(XboxGameBarWidget sender, object args)
        {
            await SetWidgetOpacity();
        }

        public async Task SetWidgetOpacity()
        {
            Log.Info("Setting opacity {opacity}", widget.RequestedOpacity);

            await WidgetPage.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (WidgetPage.Background != null)
                {
                    WidgetPage.Background.Opacity = widget.RequestedOpacity;
                }
            });
        }

        private void UntrackItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem button && button.Tag is TrackedEntry trackedEntry)
            {
                RemoveTrackedEntry(trackedEntry);
            }
        }

        private void ClearAllItems_OnClick(object sender, RoutedEventArgs e)
        {
            AppState.Data.TrackedEntries = new List<TrackedEntry>();
        }

        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private int konamiPosition = 0;
        private readonly VirtualKey[] konamiKeys = { VirtualKey.Up, VirtualKey.Up, VirtualKey.Down, VirtualKey.Down, VirtualKey.Left, VirtualKey.Right, VirtualKey.Left, VirtualKey.Right, VirtualKey.B, VirtualKey.A, VirtualKey.Enter };

        private void Grid_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (konamiKeys.Contains(e.Key))
            {
                if (e.Key == konamiKeys[konamiPosition])
                    konamiPosition += 1;
                else
                    konamiPosition = 0;

                if (konamiPosition == konamiKeys.Length)
                {
                    konamiPosition = 0;
                    Log.Info("Toggling bust profile mode");
                    AppState.Data.BustProfileRequests.Value = !AppState.Data.BustProfileRequests.Value;
                }
            }
        }

        private async void ForceRefresh_OnClick(object sender, RoutedEventArgs e)
        {
            await AppState.Data.ForceProfileUpdate();
        }

        private void ShowDescription_OnClick(object sender, RoutedEventArgs e)
        {
            if (!(sender is MenuFlyoutItem button) || !(button.Tag is TrackedEntry trackedEntry)) return;

            var item = TrackedSource.FindTrackable(trackedEntry.UniqueKey);
            if (item == null) return;

            trackedEntry.ShowDescription = !(trackedEntry.ShowDescription);
            AppState.SaveTrackedEntries(AppState.Data.TrackedEntries); // we modified in place, so just serialise and save
        }

        private async void CaptureScreenshot_OnClick(object sender, RoutedEventArgs e)
        {
            await Task.Delay(2000);

            var rtb = new RenderTargetBitmap();
            await rtb.RenderAsync(WidgetPage);

            var pixelBuffer = await rtb.GetPixelsAsync();
            var pixels = pixelBuffer.ToArray();
            var displayInformation = DisplayInformation.GetForCurrentView();
            var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-fff") + ".png", CreationCollisionOption.ReplaceExisting);
            using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Premultiplied,
                    (uint)rtb.PixelWidth,
                    (uint)rtb.PixelHeight,
                    displayInformation.RawDpiX,
                    displayInformation.RawDpiY,
                    pixels);
                await encoder.FlushAsync();
            }

            await CommonHelpers.TextToSpeech("Done.");
        }
    }

    public class TrackablesTemplateSelector : DataTemplateSelector
    {
        public DataTemplate CrucibleMapTemplate { get; set; }
        public DataTemplate TrackableEntryTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            if (item is CrucibleMapTrackable)
                return CrucibleMapTemplate;

            return TrackableEntryTemplate;
        }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            return SelectTemplateCore(item);
        }

    }
}
