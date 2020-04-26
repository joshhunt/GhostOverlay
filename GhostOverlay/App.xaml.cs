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
            this.UnhandledException += (sender, e) =>
            {
                System.Diagnostics.Debug.WriteLine("my unhandled exception handler");
                e.Handled = true;
                System.Diagnostics.Debug.WriteLine(e.Exception);
                System.Diagnostics.Debug.WriteLine(e.Exception.StackTrace);
                System.Diagnostics.Debug.WriteLine(e.ToString());
                System.Diagnostics.Debug.WriteLine(sender.ToString());
            };

            Debug.WriteLine("App constructor");

            Definitions.InitializeDatabase();
            AppState.RestoreBungieTokenDataFromSettings();
            AppState.WidgetData.RestoreTrackedBountiesFromSettings();

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
                    LaunchMainApp();
                    break;

                default:
                    Debug.WriteLine("App was activated with unknown scheme");
                    break;
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
                widgetRootFrame.Navigate(typeof(WidgetMainView), widgetMain);

            }
            else if (widgetArgs.AppExtensionId == "WidgetMainSettings")
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
                appRootFrame.Navigate(typeof(AppAuthSuccessfulView));
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