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

        /// <summary>
        ///     Initializes the singleton application object.  This is the first line of authored code
        ///     executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            Definitions.InitializeDatabase();
            AppState.RestoreBungieTokenDataFromSettings();
            AppState.WidgetData.RestoreTrackedBountiesFromSettings();

            InitializeComponent();

            Suspending += OnSuspending;
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            if (args.Kind != ActivationKind.Protocol) return;

            var protocolArgs = args as IProtocolActivatedEventArgs;
            var scheme = protocolArgs.Uri.Scheme;
            Debug.WriteLine($"app was activated with scheme {scheme}");

            if (scheme.Equals("ms-gamebarwidget")) HandleGameBarWidgetActivation(args);

            if (scheme.Equals("ghost-overlay"))
            {
                var path = protocolArgs.Uri.AbsolutePath;

                if (path.Equals("/oauth-return"))
                {
                    var parsed = HttpUtility.ParseQueryString(protocolArgs.Uri.Query);
                    var authCode = parsed["code"];
                    HandleAuthCode(authCode);
                }
            }
        }

        private void HandleGameBarWidgetActivation(IActivatedEventArgs args)
        {
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

                if (AppState.TokenData.IsValid())
                    widgetRootFrame.Navigate(typeof(WidgetMainView), widgetMain);
                else
                    widgetRootFrame.Navigate(typeof(WidgetNotAuthedView), widgetMain);

            } else if (widgetArgs.AppExtensionId == "WidgetMainSettings")
            {
                widgetMainSettings = new XboxGameBarWidget(
                    widgetArgs,
                    Window.Current.CoreWindow,
                    widgetRootFrame
                );

                Window.Current.Closed += WidgetMainSettingsWindow_Closed;

                widgetRootFrame.Navigate(typeof(WidgetSettingsView), widgetMainSettings);
            }
            
            Window.Current.Activate();
        }

        private async void HandleAuthCode(string authCode)
        {
            Debug.WriteLine($"handling auth code {authCode}");
            await AppState.bungieApi.GetOAuthAccessToken(authCode);

            Debug.WriteLine($"saved access token?: {AppState.TokenData}");

            if (AppState.TokenData.IsValid() != true)
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

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            appRootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,

            if (appRootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                appRootFrame = new Frame();
                appRootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = appRootFrame;
            }
            // just ensure that the window is active

            if (e.PrelaunchActivated == false)
            {
                if (appRootFrame.Content == null)
                {
                    if (AppState.TokenData.IsValid())
                        appRootFrame.Navigate(typeof(AppAuthSuccessfulView));
                    else
                        appRootFrame.Navigate(typeof(MainPage));
                }

                // Ensure the current window is active
                Window.Current.Activate();
            }
        }

        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }
    }
}