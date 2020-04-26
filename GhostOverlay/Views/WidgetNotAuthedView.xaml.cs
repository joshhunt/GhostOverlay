using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Microsoft.Gaming.XboxGameBar;
using Microsoft.Gaming.XboxGameBar.Authentication;

namespace GhostOverlay
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class WidgetNotAuthedView : Page
    {
        private XboxGameBarWidget widget = null;
        private XboxGameBarWebAuthenticationBroker gameBarWebAuth;

        public WidgetNotAuthedView()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            widget = e.Parameter as XboxGameBarWidget;
            gameBarWebAuth = new XboxGameBarWebAuthenticationBroker(widget);

            if (widget == null)
            {
                Debug.WriteLine("Widget parameter is null");
                return;
            }

            Debug.WriteLine("WidgetMainView OnNavigatedTo setting widget settings");
            widget.MaxWindowSize = new Size(1500, 1500);
            widget.MinWindowSize = new Size(200, 100);
            widget.HorizontalResizeSupported = true;
            widget.VerticalResizeSupported = true;
        }

        public void LoginWithDesktopBrowser_OnClick(object sender, RoutedEventArgs e)
        {
        }
    }
}
