using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace GhostOverlay
{
    public sealed partial class WidgetSettingsBountiesView : Page
    {
        private readonly RangeObservableCollection<Bounty> Bounties = new RangeObservableCollection<Bounty>();

        public WidgetSettingsBountiesView()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            GetDataAsync();
        }

        public async void GetDataAsync()
        {
            var profile = await AppState.bungieApi.GetProfileForCurrentUser(BungieApi.DefaultProfileComponents);
            Bounty.BountiesFromProfile(profile);
        }
    }
}