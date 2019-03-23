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
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using TabbedShell.Classes;
using TabbedShell.ContextMenus;
using TabbedShell.Controls;
using TabbedShell.Helpers;
using TabbedShell.Model;
using TabbedShell.Model.ContextMenu;
using TabbedShell.Win32.Interop;
using TabbedShell.Win32.Structs;

namespace TabbedShell
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public HostedWindowItem CurrentContainedWindow => TabsContainer.ActiveTabIndex == -1 ? null : TabsContainer.Tabs[TabsContainer.ActiveTabIndex].HostedWindowItem;
        public IntPtr CurrentContainedWindowHandle => CurrentContainedWindow?.WindowHandle ?? IntPtr.Zero;

        bool switchToContentEnabled = true;

        private Point? initialAbsolutePosition = null;

        public MainWindow()
        {
            InitializeComponent();

            (App.Current as App).MainWindows.Add(this);

            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render, (Action)(() =>
            {
                if (Properties.Settings.Default.NewTabDefaultIndex == 0)
                    StartProcess("cmd.exe");
                else
                    (AppContextMenus.NewTabContextMenu.Items[Properties.Settings.Default.NewTabDefaultIndex - 1] as ContextMenuClickableItem)?.Action?.Invoke(this);
            }));
        }

        public MainWindow(IntPtr windowHandle, string title)
        {
            InitializeComponent();

            (App.Current as App).MainWindows.Add(this);

            CreateTabForWindow(windowHandle, title);
        }

        /// <param name="position">In absolute pixels</param>
        /// <param name="size">In logical pixels</param>
        public MainWindow(Point position, Size size)
            : this()
        {
            initialAbsolutePosition = position;
            this.Width = size.Width;
            this.Height = size.Height;
        }

        /// <param name="position">In absolute pixels</param>
        /// <param name="size">In logical pixels</param>
        public MainWindow(IntPtr windowHandle, string title, Point position, Size size) 
            : this(windowHandle, title)
        {
            initialAbsolutePosition = position;
            this.Width = size.Width;
            this.Height = size.Height;
        }

        /// <param name="position">In absolute pixels</param>
        public MainWindow(Point position)
            : this()
        {
            initialAbsolutePosition = position;
        }

        /// <param name="position">In absolute pixels</param>
        public MainWindow(IntPtr windowHandle, string title, Point position)
            : this(windowHandle, title)
        {
            initialAbsolutePosition = position;
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            this.EnableAcrylicBlur();
            if (initialAbsolutePosition.HasValue)
            {
                var interop = new WindowInteropHelper(this);
                Win32Functions.SetWindowPos(interop.Handle, IntPtr.Zero, (int)initialAbsolutePosition.Value.X, (int)initialAbsolutePosition.Value.Y,
                    0, 0, Win32.Enums.SetWindowPosFlags.IgnoreZOrder | Win32.Enums.SetWindowPosFlags.IgnoreResize | Win32.Enums.SetWindowPosFlags.ShowWindow);   
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            OnResize();
        }

        public async void StartProcess(string procName)
        {
            try
            {
                await (App.Current as App).WindowContainSemaphore.WaitAsync();

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

                await CreateTabForWindow(process.MainWindowHandle, DefaultWindowNames.GetName(procName));
            }
            finally
            {
                (App.Current as App).WindowContainSemaphore.Release();
            }
        }

        private async Task CreateTabForWindow(IntPtr handle, string title)
        {
            await Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render, (Action)(() =>
            {
                var windowItem = new HostedWindowItem
                {
                    WindowHandle = handle,
                };
                var tabItem = new Model.UI.TabItem
                {
                    IsActive = true,
                    Title = title,
                    HostedWindowItem = windowItem,
                };
                windowItem.TabItem = tabItem;
                this.Title = title;

                TabsContainer.AddTab(tabItem);

                this.ForcePaint();
            }));
        }

        private async void AttachToWindow(HostedWindowItem item)
        {
            try
            {
                await ContainTargetWindow(item);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                TabsContainer.CloseTab(item);
            }

            try
            {
                if (Properties.Settings.Default.TransparentTerminalEnabled)
                {
                    SetWindowOpacity(item.WindowHandle, Properties.Settings.Default.TerminalTransparencyAmount);
                }
            }
            catch { }
        }

        private void SetWindowOpacity(IntPtr containedWindowHandle, double opacity)
        {
            Win32Functions.SetWindowLongPtr(new HandleRef(this, containedWindowHandle), Win32Functions.GWL_EXSTYLE, new IntPtr(Win32Functions.GetWindowLongPtr(containedWindowHandle, Win32Functions.GWL_EXSTYLE).ToInt32() | Win32Functions.WS_EX_LAYERED));
            Win32Functions.SetLayeredWindowAttributes(containedWindowHandle, 0, (byte)(opacity * 255), Win32Functions.LWA_ALPHA);
        }

        public void WindowOpacityUpdated()
        {
            if (Properties.Settings.Default.TransparentTerminalEnabled)
            {
                SetWindowOpacity(CurrentContainedWindowHandle, Properties.Settings.Default.TerminalTransparencyAmount);
            }
            else
            {
                SetWindowOpacity(CurrentContainedWindowHandle, 1.0);
            }
        }

        private async void Window_Activated(object sender, EventArgs e)
        {
            Debug.WriteLine("Activated");

            await Task.Delay(100);

            if (!switchToContentEnabled)
                return;

            if (AppContextMenus.IsAContextMenuOpen || SettingsWindow.IsOpen)
                return;

            Win32Functions.SetForegroundWindow(CurrentContainedWindowHandle);
        }

        private async Task ContainTargetWindow(HostedWindowItem item)
        {
            var target = item.WindowHandle;

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

            if (host.Parent == null)
            {
                WindowContainer.Child = host;
            }
            else if (host.Parent != null && host.Parent != WindowContainer)
            { 
                MessageBox.Show("ContainTargetWindow failed. The host has another parent.");
                return;
            }

            // TODO: Try to make it faster on console launch. But how?
            var dpi = VisualTreeHelper.GetDpi(this);
            if (item.Dpi != dpi.PixelsPerInchX)
            {
                ConsoleFunctions.SendDpiChangedMessage(target, (int)dpi.PixelsPerInchX);
                item.Dpi = dpi.PixelsPerInchX;
            }

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

                if (foregroundWindow == CurrentContainedWindowHandle)
                    this.Activate();
            }
        }

        private async void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            switchToContentEnabled = true;
            Debug.WriteLine("MouseLeave");
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
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowStyle = WindowStyle.SingleBorderWindow;
                this.WindowState = WindowState.Maximized;
                this.WindowStyle = WindowStyle.None;
            }
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
            AppContextMenus.ThreeDotContextMenu.ShowContextMenu(this, () => this);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            (App.Current as App).MainWindows.Remove(this);

            if ((App.Current as App).MainWindows.Count == 0
                && !Properties.Settings.Default.AttachToAllTerminalsEnabled)
            {
                Application.Current.Shutdown();
            }
        }

        private void TabsContainer_TabActivated(object sender, Controls.TabActivatedEventArgs e)
        {
            AttachToWindow(e.Tab.HostedWindowItem);
        }

        private void TabsContainer_TabClosing(object sender, Controls.TabCloseEventArgs e)
        {
            CloseWindowProcess(e.WindowHandle);
            if (e.RemainingTabs == 0)
            {
                CloseWindow(sendCloseRequest: true);
            }
        }

        private void TabsContainer_TabNewWindowRequested(object sender, Controls.TabNewWindowRequestEventArgs e)
        {
            var newWindow = new MainWindow(e.Tab.HostedWindowItem.WindowHandle, e.Tab.Title,
                new Point(e.Position.X, e.Position.Y));
            newWindow.Show();
        }

        private void TabsContainer_TabDragBegin(object sender, EventArgs e)
        {
            if (TabsContainer.Tabs.Count == 1)
            {
                Debug.WriteLine("bye?");
                this.Hide();

                WindowContainer.Child = null;
            }
        }

        private void TabsContainer_TabDragEnd(object sender, EventArgs e)
        {
            if (TabsContainer.Tabs.Count == 0)
            {
                Debug.WriteLine("goodbye :(");
                this.Close();
            }
        }

        protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
        {
            if (CurrentContainedWindow != null)
            {
                ConsoleFunctions.SendDpiChangedMessage(CurrentContainedWindowHandle, (int)newDpi.PixelsPerInchX);
                CurrentContainedWindow.Dpi = (int)newDpi.PixelsPerInchX;
            }
            base.OnDpiChanged(oldDpi, newDpi);
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            OnResize();
        }

        private void OnResize()
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.BorderThickness = new Thickness(5);

                var target = CurrentContainedWindowHandle;
                (App.Current as App).TargetWindowHosts[target].SetWindowPosition(this);

                MaximizeRestoreButtonText.Visibility = Visibility.Visible;
                MaximizeButtonText.Visibility = Visibility.Collapsed;
            }
            else
            {
                this.BorderThickness = new Thickness(0);

                MaximizeRestoreButtonText.Visibility = Visibility.Collapsed;
                MaximizeButtonText.Visibility = Visibility.Visible;
            }

            if ((App.Current as App).TargetWindowHosts.ContainsKey(CurrentContainedWindowHandle))
                (App.Current as App).TargetWindowHosts[CurrentContainedWindowHandle].SetWindowPosition(this);
        }
    }
}
