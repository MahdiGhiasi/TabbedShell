using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using TabbedShell.Classes;
using TabbedShell.Helpers;
using TabbedShell.Win32.Enums;
using TabbedShell.Win32.Interop;

namespace TabbedShell
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public List<MainWindow> MainWindows { get; } = new List<MainWindow>();
        public Dictionary<IntPtr, MyHost> TargetWindowHosts { get; } = new Dictionary<IntPtr, MyHost>();

        private DispatcherTimer windowTitleCheckTimer;


        private Win32Functions.WinEventDelegate windowForegroundHookCallback;
        private Win32Functions.WinEventDelegate windowCreateDestroyHookCallback;

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
            this.mutex = new Mutex(false, UniqueMutexName);
            this.eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, UniqueEventName);

            // So, R# would not give a warning that this variable is not used.
            GC.KeepAlive(this.mutex);

            try
            {
                if (!mutex.WaitOne(TimeSpan.FromSeconds(1), false))
                {
                    // Notify other instance so it could bring itself to foreground.
                    this.eventWaitHandle.Set();

                    // Terminate this instance.
                    this.Shutdown();
                }
            }
            catch (AbandonedMutexException ex)
            {
                Debug.WriteLine(ex.ToString());
            }


            // Spawn a thread which will be waiting for our event
            var thread = new Thread(() =>
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

            InitWindowTitleCheckTimer();

            HookForegroundWindowEvent();
            HookCreateDestroyWindowEvent();
        }

        private void HookCreateDestroyWindowEvent()
        {
            windowCreateDestroyHookCallback = new Win32Functions.WinEventDelegate(CreateDestroyWinEventProc);

            var hook = Win32Functions.SetWinEventHook(Win32Functions.EVENT_OBJECT_CREATE, Win32Functions.EVENT_OBJECT_DESTROY,
                IntPtr.Zero, windowCreateDestroyHookCallback,
                0, 0, Win32Functions.WINEVENT_OUTOFCONTEXT);
        }

        private void HookForegroundWindowEvent()
        {
            windowForegroundHookCallback = new Win32Functions.WinEventDelegate(ForegroundWinEventProc);

            var hook = Win32Functions.SetWinEventHook(Win32Functions.EVENT_SYSTEM_FOREGROUND, Win32Functions.EVENT_SYSTEM_FOREGROUND,
                IntPtr.Zero, windowForegroundHookCallback,
                0, 0, Win32Functions.WINEVENT_OUTOFCONTEXT);
        }

        private void CreateDestroyWinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            // Close tab if process exited
            if (eventType == Win32Functions.EVENT_OBJECT_DESTROY)
            {
                var allTabs = (from window in MainWindows
                               from tab in window.TabsContainer.Tabs
                               select (window, tab)).ToList();

                foreach (var (window, tab) in allTabs)
                    if (tab.HostedWindowItem.WindowHandle == hwnd)
                            window.TabsContainer.CloseTab(tab.HostedWindowItem);
            }

            // Attach to new terminals
            if (eventType == Win32Functions.EVENT_OBJECT_CREATE && TabbedShell.Properties.Settings.Default.AttachToAllTerminalsEnabled)
            {
                // TODO
            }
        }

        private void ForegroundWinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            // Makes sure relevant MainWindow comes to front when user clicks on the the terminal window (not on the border or tab bar)
            var allTabs = (from window in MainWindows
                           from tab in window.TabsContainer.Tabs
                           select (window, tab)).ToList();

            foreach (var (window, tab) in allTabs)
                if (tab.HostedWindowItem.WindowHandle == hwnd)
                {
                    // Bring window to front without activating it
                    // https://stackoverflow.com/a/14211193/942659
                    Win32Functions.SetWindowPos((new WindowInteropHelper(window)).Handle,
                        Win32Functions.HWND_TOP, 0, 0, 0, 0, SetWindowPosFlags.IgnoreMove | SetWindowPosFlags.IgnoreResize | SetWindowPosFlags.DoNotActivate);
                }
        }

        private void InitWindowTitleCheckTimer()
        {
            windowTitleCheckTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1),
            };
            windowTitleCheckTimer.Tick += WindowTitleCheckTimer_Tick;
            windowTitleCheckTimer.Start();
        }

        private void WindowTitleCheckTimer_Tick(object sender, EventArgs e)
        {
            foreach (var window in MainWindows)
            {
                foreach (var tab in window.TabsContainer.Tabs)
                {
                    try
                    {
                        tab.Title = DefaultWindowNames.NormalizeTitle(Win32Functions.GetWindowText(tab.HostedWindowItem.WindowHandle));
                    }
                    catch { }
                }
            }
        }
    }
}
