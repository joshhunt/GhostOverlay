using System;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace GhostOverlay
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            Debug.WriteLine("MainPage is running!");
        }

        protected override void OnNavigatedTo(NavigationEventArgs ev)
        {
            base.OnNavigatedTo(ev);

            Debug.WriteLine("MainPage OnNavigatedTo");
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var urlBungieAuth = new Uri(AppState.bungieApi.GetAuthorisationUrl());
            var success = await Windows.System.Launcher.LaunchUriAsync(urlBungieAuth);

            if (!success)
            {
                Debug.WriteLine("TODO: Failed to launch Bungie auth page");
            }
        }
    }

}