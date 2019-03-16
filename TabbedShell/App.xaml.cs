using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using TabbedShell.Classes;

namespace TabbedShell
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public List<MainWindow> MainWindows { get; } = new List<MainWindow>();
        public Dictionary<IntPtr, MyHost> TargetWindowHosts { get; } = new Dictionary<IntPtr, MyHost>();

        // Single instance and notifying the previous instance obtained from https://stackoverflow.com/a/23730146/942659

        private const string UniqueEventName = "428efeec-180d-4406-aa45-d04c71c250fc";
        private const string UniqueMutexName = "9fddf5c9-a5b8-491a-929a-537ca81ed578";
        private EventWaitHandle eventWaitHandle;
        private Mutex mutex;

        /// <summary>The app on startup.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            bool isOwned;
            this.mutex = new Mutex(true, UniqueMutexName, out isOwned);
            this.eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, UniqueEventName);

            // So, R# would not give a warning that this variable is not used.
            GC.KeepAlive(this.mutex);

            if (isOwned)
            {
                // Spawn a thread which will be waiting for our event
                var thread = new Thread(
                    () =>
                    {
                        while (this.eventWaitHandle.WaitOne())
                        {
                            Current.Dispatcher.BeginInvoke(
                                (Action)(() =>
                                {
                                    (new MainWindow()).Show();
                                }));
                        }
                    });

                // It is important mark it as background otherwise it will prevent app from exiting.
                thread.IsBackground = true;

                thread.Start();
                return;
            }

            // Notify other instance so it could bring itself to foreground.
            this.eventWaitHandle.Set();

            // Terminate this instance.
            this.Shutdown();
        }
    }
}
