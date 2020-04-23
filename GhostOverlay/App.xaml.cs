using System;
using System.Diagnostics;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace GhostOverlay
{
    /// <summary>
    ///     Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        private Frame appRootFrame;

        /// <summary>
        ///     Initializes the singleton application object.  This is the first line of authored code
        ///     executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.UnhandledException += (sender, e) =>
            {
                System.Diagnostics.Debug.WriteLine("my handled exception handler");
                e.Handled = true;
                System.Diagnostics.Debug.WriteLine(e.Exception);
                System.Diagnostics.Debug.WriteLine(e.Exception.StackTrace);
                System.Diagnostics.Debug.WriteLine(e.ToString());
                System.Diagnostics.Debug.WriteLine(sender.ToString());
            };

            Debug.WriteLine("App constructor");

            InitializeComponent();

            Suspending += OnSuspending;
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

            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }
    }
}