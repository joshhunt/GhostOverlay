using System;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Gaming.XboxGameBar;
using Windows.UI;
using Windows.UI.Core;
using GhostOverlay.Views;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace GhostOverlay
{
    public sealed partial class WidgetSettingsView : Page
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

                widget.RequestedThemeChanged += Widget_RequestedThemeChanged;
                Widget_RequestedThemeChanged(widget, null);

                widget.Close();
            }

            navView.SelectedItem = navView.MenuItems[0];

            AppState.Data.ScheduleProfileUpdates();
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            AppState.Data.UnscheduleProfileUpdates();
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

        private void NavView_OnBackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            Debug.WriteLine($"Back requested! contentFrame.BackStackDepth: {contentFrame.BackStackDepth}, canGoBack {contentFrame.CanGoBack}");

            if (contentFrame.CanGoBack)
            {
                contentFrame.GoBack();
            }
        }
    }
}
