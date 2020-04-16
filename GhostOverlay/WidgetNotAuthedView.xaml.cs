using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Microsoft.Gaming.XboxGameBar;

namespace GhostOverlay
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class WidgetNotAuthedView : Page
    {
        private XboxGameBarWidget widget = null;

        public WidgetNotAuthedView()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            widget = e.Parameter as XboxGameBarWidget;
        }

        public async void Button_Click(object sender, RoutedEventArgs e)
        {
            var urlBungieAuth = new Uri(AppState.bungieApi.GetAuthorisationUrl());
            var success = await Windows.System.Launcher.LaunchUriAsync(urlBungieAuth);

            if (success)
            {
                ProgressRing.IsActive = true;
                LoginButton.Visibility = Visibility.Collapsed;
                WaitingText.Visibility = Visibility.Visible;
                WaitForAuth();
            }
            else
            {
                Debug.WriteLine("TODO: Failed to launch Bungie auth page");
            } 
        }

        private async void WaitForAuth()
        {
            while (!AppState.TokenData.IsValid())
            {
                await Task.Delay(1000);
            }

            ProgressRing.IsActive = false;
            WaitingText.Text = "User logged in!!!";

            this.Frame.Navigate(typeof(WidgetMainView), widget);
        }
    }
}
