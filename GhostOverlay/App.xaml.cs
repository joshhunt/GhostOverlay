using System;
using System.Diagnostics;
using System.Web;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Gaming.XboxGameBar;

namespace GhostOverlay
{
    /// <summary>
    ///     Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        private Frame appRootFrame;
        private XboxGameBarWidget widgetMain;
        private XboxGameBarWidget widgetMainSettings;

        private readonly MyEventAggregator eventAggregator = new MyEventAggregator();

        /// <summary>
        ///     Initializes the singleton application object.  This is the first line of authored code
        ///     executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            Definitions.InitializeDatabase();
            AppState.Data.RestoreBungieTokenDataFromSettings();
            AppState.Data.RestoreTrackedBountiesFromSettings();

            InitializeComponent();

            Suspending += OnSuspending;
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            Debug.WriteLine("OnActivated");
            if (args.Kind != ActivationKind.Protocol) return;

            var protocolArgs = args as IProtocolActivatedEventArgs;
            var scheme = protocolArgs?.Uri?.Scheme ?? "";
            Debug.WriteLine($"app was activated with scheme {scheme}");

            switch (scheme)
            {
                case "ms-gamebarwidget":
                    HandleGameBarWidgetActivation(args);
                    break;

                case "ghost-overlay":
                    var path = protocolArgs?.Uri?.AbsolutePath ?? "";
                    LaunchMainApp();

                    if (path.Equals("/oauth-return"))
                    {
                        var parsed = HttpUtility.ParseQueryString(protocolArgs.Uri.Query);
                        var authCode = parsed["code"];
                        HandleAuthCode(authCode);
                    }
                    break;

                default:
                    Debug.WriteLine("App was activated with unknown scheme");
                    break;
            }
        }

        private void HandleGameBarWidgetActivation(IActivatedEventArgs args)
        {
            Debug.WriteLine("HandleGameBarWidgetActivation");
            var widgetArgs = args as XboxGameBarWidgetActivatedEventArgs;

            if (widgetArgs == null || !widgetArgs.IsLaunchActivation) return;

            Debug.WriteLine($"\n*** Game bar widget activation for {widgetArgs.AppExtensionId} ***");

            var widgetRootFrame = new Frame();
            widgetRootFrame.NavigationFailed += OnNavigationFailed;
            Window.Current.Content = widgetRootFrame;

            if (widgetArgs.AppExtensionId == "WidgetMain")
            {
                widgetMain = new XboxGameBarWidget(
                    widgetArgs,
                    Window.Current.CoreWindow,
                    widgetRootFrame
                );

                Window.Current.Closed += WidgetMainWindow_Closed;
                widgetMain.VisibleChanged += AnyWidget_VisibleChanged;

                if (AppState.Data.TokenData.IsValid())
                    widgetRootFrame.Navigate(typeof(WidgetMainView), widgetMain);
                else
                    widgetRootFrame.Navigate(typeof(WidgetNotAuthedView), widgetMain);

            }
            else if (widgetArgs.AppExtensionId == "WidgetMainSettings")
            {
                widgetMainSettings = new XboxGameBarWidget(
                    widgetArgs,
                    Window.Current.CoreWindow,
                    widgetRootFrame
                );

                Window.Current.Closed += WidgetMainSettingsWindow_Closed;
                widgetMainSettings.VisibleChanged += AnyWidget_VisibleChanged;

                widgetRootFrame.Navigate(typeof(WidgetSettingsView), widgetMainSettings);
            }

            Window.Current.Activate();
        }

        private void AnyWidget_VisibleChanged(XboxGameBarWidget sender, object args)
        {
            AppState.Data.WidgetsAreVisible = (widgetMain?.Visible ?? false) ||
                                                    (widgetMainSettings?.Visible ?? false);
            AppState.Data.WidgetVisibilityChanged();
        }

        private async void HandleAuthCode(string authCode)
        {
            Debug.WriteLine($"handling auth code {authCode}");
            await AppState.bungieApi.GetOAuthAccessToken(authCode);

            Debug.WriteLine($"saved access token?: {AppState.Data.TokenData}");

            if (AppState.Data.TokenData.IsValid() != true)
                throw new Exception("Exchanged code for token, but the TokenData is not valid??");

            appRootFrame?.Navigate(typeof(AppAuthSuccessfulView));
        }

        private void WidgetMainWindow_Closed(object sender, CoreWindowEventArgs e)
        {
            widgetMain = null;
            Window.Current.Closed -= WidgetMainWindow_Closed;
        }

        private void WidgetMainSettingsWindow_Closed(object sender, CoreWindowEventArgs e)
        {
            widgetMainSettings = null;
            Window.Current.Closed -= WidgetMainSettingsWindow_Closed;
        }

        private void LaunchMainApp()
        {
            appRootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            if (appRootFrame == null)
            {
                Debug.WriteLine("App root frame is null, making a new one");

                // Create a Frame to act as the navigation context and navigate to the first page
                appRootFrame = new Frame();
                appRootFrame.NavigationFailed += OnNavigationFailed;

                // Place the frame in the current Window
                Window.Current.Content = appRootFrame;
            }

            if (appRootFrame.Content == null)
            {
                if (AppState.Data.TokenData.IsValid())
                    appRootFrame.Navigate(typeof(WidgetSettingsView));
                    // appRootFrame.Navigate(typeof(WidgetSettingsView));
                    //  appRootFrame.Navigate(typeof(AppAuthSuccessfulView));
                else
                    appRootFrame.Navigate(typeof(MainPage));
            }

            // Ensure the current window is active
            Window.Current.Activate();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Debug.WriteLine("OnLaunched");

            LaunchMainApp();

            if (e.PrelaunchActivated)
            {
                Debug.WriteLine("PrelaunchActivated was true, i wonder if we needed to do something different here?");
            }
        }

        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            Debug.WriteLine("OnSuspending");
            var deferral = e.SuspendingOperation.GetDeferral();

            widgetMainSettings = null;
            widgetMain = null;

            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }
    }
}