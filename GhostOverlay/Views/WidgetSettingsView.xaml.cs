using System;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Microsoft.Gaming.XboxGameBar;
using NavigationView = Microsoft.UI.Xaml.Controls.NavigationView;
using muxc = Microsoft.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace GhostOverlay
{
    public sealed partial class WidgetSettingsView : Page
    {
        public WidgetSettingsView()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            navView.SelectedItem = navView.MenuItems[0];
        }

        private void NavView_OnSelectionChanged(NavigationView sender, muxc.NavigationViewSelectionChangedEventArgs args)
        {
            var item = args.SelectedItem as muxc.NavigationViewItem;
            var selectedView = item?.Tag?.ToString();

            switch (selectedView)
            {
                case "Bounties":
                    contentFrame.Navigate(typeof(WidgetSettingsBountiesView), null, args.RecommendedNavigationTransitionInfo);
                    break;

                case "Triumphs":
                    contentFrame.Navigate(typeof(WidgetSettingsTriumphsView), null, args.RecommendedNavigationTransitionInfo);
                    break;
            }
        }
    }
}
