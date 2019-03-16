using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TabbedShell.Classes;
using TabbedShell.ContextMenus;
using TabbedShell.Helpers;
using TabbedShell.Model;
using TabbedShell.Win32.Interop;
using TabbedShell.Win32.Structs;

namespace TabbedShell
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool switchToContentEnabled = true;

        public MainWindow()
        {
            InitializeComponent();           

            (App.Current as App).MainWindows.Add(this);

            Task.Run(() => StartProcess("cmd.exe", "Command Prompt"));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.EnableAcrylicBlur();
        }

        public async void StartProcess(string procName, string title = "")
        {
            var cmd = new ProcessStartInfo(procName)
            {
                WindowStyle = ProcessWindowStyle.Minimized,
                CreateNoWindow = true,
            };
            var process = Process.Start(cmd);
            while (process.MainWindowHandle == IntPtr.Zero)
            {
                await Task.Delay(10);
            }

            await Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render, (Action)(() =>
            {
                var windowItem = new HostedWindowItem
                {
                    WindowHandle = process.MainWindowHandle,
                    Title = title,
                };
                var tabItem = new Model.UI.TabItem
                {
                    IsActive = true,
                    HostedWindowItem = windowItem,
                };
                windowItem.TabItem = tabItem;

                TabsContainer.AddTab(tabItem);
            }));
        }

        private void AttachToWindow(IntPtr handle)
        {
            ContainTargetWindow(handle);
            SetWindowOpacity(handle, 0.65);
        }


        private void SetWindowOpacity(IntPtr containedWindowHandle, double opacity)
        {
            Win32Functions.SetWindowLongPtr(new HandleRef(this, containedWindowHandle), Win32Functions.GWL_EXSTYLE, new IntPtr(Win32Functions.GetWindowLongPtr(containedWindowHandle, Win32Functions.GWL_EXSTYLE).ToInt32() | Win32Functions.WS_EX_LAYERED));
            Win32Functions.SetLayeredWindowAttributes(containedWindowHandle, 0, (byte)(opacity * 255), Win32Functions.LWA_ALPHA);
        }

        private async void Window_Activated(object sender, EventArgs e)
        {
            Debug.WriteLine("Activated");

            await Task.Delay(100);

            if (!switchToContentEnabled)
                return;

            if (AppContextMenus.IsAContextMenuOpen || SettingsWindow.IsOpen)
                return;

            Win32Functions.SetForegroundWindow(TabsContainer.CurrentContainedWindowHandle);
        }

        private async void ContainTargetWindow(IntPtr target)
        {
            MyHost host;
            if ((App.Current as App).TargetWindowHosts.ContainsKey(target))
            {
                host = (App.Current as App).TargetWindowHosts[target];
            }
            else
            {
                host = new MyHost(target);
                (App.Current as App).TargetWindowHosts[target] = host;
            }

            WindowContainer.Child = host;

            await Task.Delay(10);

            int style = Win32Functions.GetWindowLongPtr(target, Win32Functions.GWL_STYLE).ToInt32();
            int newStyle = style & ~(Win32Functions.WS_CHILD);
            Win32Functions.SetWindowLongPtr(new HandleRef(this, target), Win32Functions.GWL_STYLE, new IntPtr(newStyle));

            await Task.Delay(10);

            MaximizeWindow(target, Win32.Enums.ShowWindowCommands.Normal);
            Win32Functions.SetForegroundWindow(target);
        }

        private static void MaximizeWindow(IntPtr target, Win32.Enums.ShowWindowCommands cmd)
        {
            WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
            placement.Length = Marshal.SizeOf(placement);
            Win32Functions.GetWindowPlacement(target, ref placement);

            if (placement.ShowCmd != Win32.Enums.ShowWindowCommands.Maximize)
            {
                WINDOWPLACEMENT newPlacement = new WINDOWPLACEMENT
                {
                    Length = Marshal.SizeOf(typeof(WINDOWPLACEMENT)),
                    ShowCmd = cmd,
                    MaxPosition = placement.MaxPosition,
                    MinPosition = placement.MinPosition,
                    NormalPosition = placement.NormalPosition,
                };
                Win32Functions.SetWindowPlacement(target, ref newPlacement);
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine("MouseLeftButtonDown");
            switchToContentEnabled = false;
            DragMove();
        }

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            switchToContentEnabled = true;
            Window_Activated(this, new EventArgs());
        }

        private async void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            switchToContentEnabled = false;
            Debug.WriteLine("MouseEnter");

            await Task.Delay(100);
            if (!AppContextMenus.IsAContextMenuOpen && !SettingsWindow.IsOpen)
            {
                IntPtr foregroundWindow = Win32Functions.GetForegroundWindow();

                if (foregroundWindow == TabsContainer.CurrentContainedWindowHandle)
                    this.Activate();
            }
        }

        private async void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            switchToContentEnabled = true;
            Debug.WriteLine("MouseLeave");

            await Task.Delay(100);
            if (AppContextMenus.IsAContextMenuOpen || SettingsWindow.IsOpen)
                return;

            Win32Functions.SetForegroundWindow(TabsContainer.CurrentContainedWindowHandle);
        }

        private void CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            CloseWindow(sendCloseRequest: false);
        }

        private void CloseWindow(bool sendCloseRequest)
        {
            foreach (var tab in TabsContainer.Tabs)
            {
                CloseWindowProcess(tab.HostedWindowItem.WindowHandle);
            }

            if (sendCloseRequest)
                this.Close();
        }

        private void MaximizeWindow_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Maximized;
        }

        private void MinimizeWindow_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void CloseWindowProcess(IntPtr windowHandle)
        {
            var myProcId = Process.GetCurrentProcess().Id;

            Win32Functions.GetWindowThreadProcessId(windowHandle, out uint consoleRootProcessId);

            // https://stackoverflow.com/a/28616832/942659
            // TODO: Create console event handler for reliability

            Win32Functions.AttachConsole(consoleRootProcessId);
            uint[] procIds = new uint[1024];
            Win32Functions.GetConsoleProcessList(procIds, 1024);
            Win32Functions.FreeConsole();

            foreach (var procId in procIds)
            {
                if (procId == 0)
                    continue;
                if (procId == myProcId)
                    continue;

                try
                {
                    var process = Process.GetProcessById((int)procId);
                    process.Kill();
                }
                catch { }
            }
        }

        private void ThreeDotsMenu_Click(object sender, RoutedEventArgs e)
        {
            AppContextMenus.ThreeDotContextMenu.ShowContextMenu(this);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            (App.Current as App).MainWindows.Remove(this);

            if ((App.Current as App).MainWindows.Count == 0)
                Application.Current.Shutdown();
        }

        private void TabsContainer_TabActivated(object sender, Controls.TabActivatedEventArgs e)
        {
            AttachToWindow(e.WindowHandle);
        }

        private void TabsContainer_TabClosing(object sender, Controls.TabCloseEventArgs e)
        {
            CloseWindowProcess(e.WindowHandle);
            if (e.RemainingTabs == 0)
            {
                CloseWindow(sendCloseRequest: true);
            }
        }
    }
}
