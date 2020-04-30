using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Gaming.XboxGameBar;
using System.ComponentModel;
using Windows.UI;
using Windows.UI.Core;
using NavigationView = Microsoft.UI.Xaml.Controls.NavigationView;
using NavigationViewSelectionChangedEventArgs = Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs;
using muxc = Microsoft.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace GhostOverlay
{
    public sealed partial class WidgetSettingsView : Page
    {
        private XboxGameBarWidget widget;

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
            }

            navView.SelectedItem = navView.MenuItems[0];

            AppState.WidgetData.ScheduleProfileUpdates();
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            AppState.WidgetData.UnscheduleProfileUpdates();
        }

        private void NavView_OnSelectionChanged(NavigationView sender, muxc.NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                throw new NotImplementedException();
            }

            Debug.WriteLine($"Before Clicking a nav item, contentFrame.BackStackDepth: {contentFrame.BackStackDepth}, canGoBack {contentFrame.CanGoBack}");

            var item = args.SelectedItem as muxc.NavigationViewItem;
            var selectedView = item?.Tag.ToString();

            switch (selectedView)
            {
                case "Bounties":
                    contentFrame.Navigate(typeof(WidgetSettingsBountiesView), null, args.RecommendedNavigationTransitionInfo);
                    break;

                case "Triumphs":
                    contentFrame.Navigate(typeof(WidgetSettingsTriumphsView), null, args.RecommendedNavigationTransitionInfo);
                    break;
            }

            contentFrame.BackStack.Clear();
            Debug.WriteLine($"After Clicking a nav item, contentFrame.BackStackDepth: {contentFrame.BackStackDepth}, canGoBack {contentFrame.CanGoBack}");
        }

        private void GoToBounties_Click(object sender, RoutedEventArgs e)
        {
            contentFrame.Navigate(typeof(WidgetSettingsBountiesView));
            Debug.WriteLine($"After Clicking a nav item, contentFrame.BackStackDepth: {contentFrame.BackStackDepth}, canGoBack {contentFrame.CanGoBack}");
        }

        private void GoToTriumphs_Click(object sender, RoutedEventArgs e)
        {
            contentFrame.Navigate(typeof(WidgetSettingsTriumphsView));
            Debug.WriteLine($"After Clicking a nav item, contentFrame.BackStackDepth: {contentFrame.BackStackDepth}, canGoBack {contentFrame.CanGoBack}");
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

        private void NavView_OnBackRequested(NavigationView sender, muxc.NavigationViewBackRequestedEventArgs args)
        {
            Debug.WriteLine($"Back requested! contentFrame.BackStackDepth: {contentFrame.BackStackDepth}, canGoBack {contentFrame.CanGoBack}");
            if (contentFrame.CanGoBack)
            {
                contentFrame.GoBack();
                return;
            }

            return;
        }
    }
}
