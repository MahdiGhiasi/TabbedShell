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
        ObservableCollection<Model.UI.TabItem> Tabs { get; } = new ObservableCollection<Model.UI.TabItem>();
        int ActiveTabIndex => Tabs.IndexOf(Tabs.FirstOrDefault(x => x.IsActive));
        IntPtr CurrentContainedWindowHandle => ActiveTabIndex == -1 ? IntPtr.Zero : Tabs[ActiveTabIndex].HostedWindowItem.WindowHandle;

        Dictionary<IntPtr, MyHost> hosts = new Dictionary<IntPtr, MyHost>();

        bool switchToContentEnabled = true;
        SemaphoreSlim tabCloseSemaphore = new SemaphoreSlim(1, 1);

        public MainWindow()
        {
            InitializeComponent();

            TabsList.ItemsSource = Tabs;
            Tabs.CollectionChanged += Tabs_CollectionChanged;

            (App.Current as App).MainWindows.Add(this);

            Task.Run(() => StartProcess("cmd.exe", "Command Prompt"));
        }

        private void Tabs_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            SetTabReferenceSize();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetTabReferenceSize();
        }

        private void SetTabReferenceSize()
        {
            SetTabReferenceSize(Tabs.Count);
        }

        private void SetTabReferenceSize(int tabsCount)
        {
            var da = new DoubleAnimation(Math.Min(201, ((TabsContainer.ColumnDefinitions[0].ActualWidth - NewTab.ActualWidth) / tabsCount) - 1), TimeSpan.FromSeconds(0.2));
            da.EasingFunction = new ExponentialEase();
            TabReferenceSize.BeginAnimation(Grid.WidthProperty, da);
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
                Tabs.Add(tabItem);

                ActivateTab(Tabs.Count - 1);
            }));
        }

        private void AttachToWindow(IntPtr handle)
        {
            ContainTargetWindow(handle);
            SetWindowOpacity(handle, 0.65);
        }

        private void ActivateTab(int index)
        {
            for (int i = 0; i < Tabs.Count; i++)
                Tabs[i].IsActive = (i == index);

            AttachToWindow(Tabs[index].HostedWindowItem.WindowHandle);
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

            Win32Functions.SetForegroundWindow(CurrentContainedWindowHandle);
        }

        private async void ContainTargetWindow(IntPtr target)
        {
            MyHost host;
            if (hosts.ContainsKey(target))
            {
                host = hosts[target];
            }
            else
            {
                host = new MyHost(target);
                hosts[target] = host;
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

                if (foregroundWindow == CurrentContainedWindowHandle)
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

            Win32Functions.SetForegroundWindow(CurrentContainedWindowHandle);
        }

        private void NewTab_Click(object sender, RoutedEventArgs e)
        {
            if (Properties.Settings.Default.NewTabDefaultIndex == 0)
                AppContextMenus.NewTabContextMenu.ShowContextMenu(this);
            else
                AppContextMenus.NewTabContextMenu.Items[Properties.Settings.Default.NewTabDefaultIndex - 1].Action?.Invoke(this);
        }

        private void Tab_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Tab");
            var hostedWindowItem = (sender as Control).Tag as HostedWindowItem;

            var index = Tabs.IndexOf(hostedWindowItem.TabItem);
            ActivateTab(index);
        }

        private void Tab_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle && e.MiddleButton == MouseButtonState.Released)
            {
                var hostedWindowItem = (sender as Control).Tag as HostedWindowItem;
                CloseTab(hostedWindowItem);
            }
        }

        private void TabClose_Click(object sender, RoutedEventArgs e)
        {
            var hostedWindowItem = (sender as Control).Tag as HostedWindowItem;
            CloseTab(hostedWindowItem);
        }

        private async void CloseTab(HostedWindowItem hostedWindowItem)
        {
            await tabCloseSemaphore.WaitAsync();

            try
            {

                CloseWindowProcess(hostedWindowItem.WindowHandle);

                var index = Tabs.IndexOf(hostedWindowItem.TabItem);
                var activeIndex = ActiveTabIndex;

                Tabs[index].Exiting = true;

                SetTabReferenceSize(Tabs.Count - 1);

                if (Tabs.Count == 1)
                {
                    CloseWindow(sendCloseRequest: true);
                }
                else if (activeIndex == index)
                {
                    if (index == Tabs.Count - 1)
                        ActivateTab(Tabs.Count - 2);
                    else
                        ActivateTab(index + 1);
                }

                await Task.Delay(200);
                Tabs.RemoveAt(index);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception in CloseTab: " + ex.ToString());
            }
            finally
            {
                tabCloseSemaphore.Release();
            }
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
            foreach (var tab in Tabs)
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

        private void TabsList_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (sender is ListBoxItem && e.LeftButton == MouseButtonState.Pressed)
            {
                ListBoxItem draggedItem = sender as ListBoxItem;
                (draggedItem.Content as Model.UI.TabItem).Exiting = true;
                DragDrop.DoDragDrop(draggedItem, draggedItem.DataContext, DragDropEffects.Move);
                draggedItem.IsSelected = true;

                // TODO: When dropped outside, tab disappears. (Exiting remains true)
            }
        }

        private void TabsList_Drop(object sender, DragEventArgs e)
        {
            Model.UI.TabItem droppedData = e.Data.GetData(typeof(Model.UI.TabItem)) as Model.UI.TabItem;
            Model.UI.TabItem target = ((ListBoxItem)(sender)).DataContext as Model.UI.TabItem;

            var addNext = (e.GetPosition((ListBoxItem)sender).X > ((ListBoxItem)sender).ActualWidth / 2);

            droppedData.Exiting = false;

            int removedIdx = Tabs.IndexOf(droppedData);
            int targetIdx = Tabs.IndexOf(target);

            if (addNext)
            {
                Tabs.Insert(targetIdx + 1, droppedData);
                Tabs.RemoveAt(removedIdx);
            }
            else
            {
                int remIdx = removedIdx + 1;
                if (Tabs.Count + 1 > remIdx)
                {
                    Tabs.Insert(targetIdx, droppedData);
                    Tabs.RemoveAt(remIdx);
                }
            }

        }
    }
}
