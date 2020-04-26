using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Gaming.XboxGameBar;

namespace GhostOverlay
{
    public sealed partial class WidgetSettingsBountiesView : Page, ISubscriber<PropertyChanged>
    {
        private readonly RangeObservableCollection<Bounty> Bounties = new RangeObservableCollection<Bounty>();
        private string definitionsDbName = "<not loaded>";
        private XboxGameBarWidget widget;
        private readonly MyEventAggregator eventAggregator = new MyEventAggregator();

        public WidgetSettingsBountiesView()
        {
            InitializeComponent();
            eventAggregator.Subscribe(this);
        }

        public void HandleMessage(PropertyChanged message)
        {
            switch (message)
            {
                case PropertyChanged.Profile:
                case PropertyChanged.DefinitionsPath:
                    UpdateViewModel();
                    break;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            widget = e.Parameter as XboxGameBarWidget;
            AppState.WidgetData.ScheduleProfileUpdates();
            UpdateViewModel();
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            eventAggregator.Subscribe(this);
            AppState.WidgetData.UnscheduleProfileUpdates();
        }

        private void UpdateViewModel()
        {
            definitionsDbName = Path.GetFileNameWithoutExtension(AppState.WidgetData.DefinitionsPath ?? "");

            var profile = AppState.WidgetData.Profile;
            if (profile?.CharacterInventories?.Data != null && AppState.WidgetData.DefinitionsLoaded)
            {
                Bounties.Clear();
                Bounties.AddRange(Bounty.BountiesFromProfile(profile));

                BountiesCollection.Source =
                    from t in Bounties
                    orderby t.AllObjectivesComplete
                    group t by t.OwnerCharacter
                    into g
                    select g;
            }
        }
    }
}