using System;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Microsoft.Gaming.XboxGameBar;
using GhostOverlay.Views;

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

            navView.SelectedItem = navView.MenuItems[0];

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
            Debug.WriteLine($"[WidgetSettingsView] HandleMessage {message}");

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
                contentFrame.Navigate(typeof(WidgetSettingsSettingsView), null,
                    args.RecommendedNavigationTransitionInfo);
                return;
            }

            var item = args.SelectedItem as NavigationViewItem;
            var selectedView = item?.Tag.ToString();

            switch (selectedView)
            {
                case "Bounties":
                    contentFrame.Navigate(typeof(WidgetSettingsBountiesView), null, args.RecommendedNavigationTransitionInfo);
                    break;

                case "Triumphs":
                    contentFrame.Navigate(typeof(SettingsRootTriumphsView), contentFrame, args.RecommendedNavigationTransitionInfo);
                    break;
            }
        }

        private void NavView_OnBackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            if (contentFrame.CanGoBack)
            {
                contentFrame.GoBack();
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
