using System;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Microsoft.Gaming.XboxGameBar;
using GhostOverlay.Views;

using NavigationViewItem = Microsoft.UI.Xaml.Controls.NavigationViewItem;
using NavigationView = Microsoft.UI.Xaml.Controls.NavigationView;
using NavigationViewBackRequestedEventArgs = Microsoft.UI.Xaml.Controls.NavigationViewBackRequestedEventArgs;
using NavigationViewSelectionChangedEventArgs = Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs;

namespace GhostOverlay
{
    public sealed partial class WidgetSettingsView : Page, ISubscriber<WidgetPropertyChanged>
    {
        private XboxGameBarWidget widget;
        private readonly MyEventAggregator eventAggregator = new MyEventAggregator();

        public WidgetSettingsView()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            widget = e.Parameter as XboxGameBarWidget;

            if (widget != null)
            {
                widget.MaxWindowSize = new Size(1940, 2000);
                widget.MinWindowSize = new Size(200, 100);
                widget.HorizontalResizeSupported = true;
                widget.VerticalResizeSupported = true;
                widget.SettingsSupported = false;
            }

            NavView.SelectedItem = NavView.MenuItems[0];

            eventAggregator.Subscribe(this);
            AppState.Data.ScheduleProfileUpdates();
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            AppState.Data.UnscheduleProfileUpdates();
        }

        public void HandleMessage(WidgetPropertyChanged message)
        {
            //Debug.WriteLine($"[WidgetSettingsView] HandleMessage {message}");
            switch (message)
            {
                case WidgetPropertyChanged.TokenData:
                    CheckAuth();
                    break;
            }
        }

        private void NavView_OnSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                ContentFrame.Navigate(typeof(WidgetSettingsSettingsView), null,
                    args.RecommendedNavigationTransitionInfo);
                return;
            }

            var item = args.SelectedItem as NavigationViewItem;
            var selectedView = item?.Tag.ToString();

            switch (selectedView)
            {
                case "Bounties":
                    ContentFrame.Navigate(typeof(WidgetSettingsBountiesView), null, args.RecommendedNavigationTransitionInfo);
                    break;

                case "Triumphs":
                    ContentFrame.Navigate(typeof(SettingsRootTriumphsView), ContentFrame, args.RecommendedNavigationTransitionInfo);
                    break;
            }
        }

        private void NavView_OnBackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs navigationViewBackRequestedEventArgs)
        {
            if (ContentFrame.CanGoBack)
            {
                ContentFrame.GoBack();
            }
        }

        private async void CheckAuth()
        {
            if (AppState.Data.TokenData == null || !AppState.Data.TokenData.IsValid())
            {
                var widgetControl = new XboxGameBarWidgetControl(widget);
                await widgetControl.CloseAsync("WidgetMainSettings");
            }
        }

        private void Log(string message)
        {
            Debug.WriteLine($"[WidgetSettingsView] {message}");
        }
    }
}
