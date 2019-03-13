﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TabbedShell.Classes;
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
        FullyObservableCollection<Model.UI.TabItem> Tabs { get; } = new FullyObservableCollection<Model.UI.TabItem>();
        int ActiveTabIndex => Tabs.IndexOf(Tabs.FirstOrDefault(x => x.IsActive));
        IntPtr CurrentContainedWindowHandle => ActiveTabIndex == -1 ? IntPtr.Zero : Tabs[ActiveTabIndex].HostedWindowItem.WindowHandle;

        Dictionary<IntPtr, MyHost> hosts = new Dictionary<IntPtr, MyHost>();

        bool switchToContentEnabled = true;

        public MainWindow()
        {
            InitializeComponent();

            TabsList.ItemsSource = Tabs;
            Tabs.CollectionChanged += Tabs_CollectionChanged;
        }

        private void Tabs_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            TabReferenceSize.Width = Math.Min(201, ((TabsContainer.ActualWidth - NewTab.ActualWidth) / Tabs.Count) - 1);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.EnableAcrylicBlur();

            foreach (var item in Process.GetProcessesByName("conhost"))
            {
                var handle = item.MainWindowHandle;
                Debug.WriteLine(handle);
            }

            StartCmd();
        }

        private async void StartCmd()
        {
            var cmd = new ProcessStartInfo("cmd.exe")
            {
                WindowStyle = ProcessWindowStyle.Minimized,
                CreateNoWindow = true,
            };
            var process = Process.Start(cmd);
            while (process.MainWindowHandle == IntPtr.Zero)
            {
                await Task.Delay(10);
                Debug.WriteLine("Waiting...");
            }

            var windowItem = new HostedWindowItem
            {
                WindowHandle = process.MainWindowHandle,
                Title = $"Command Prompt {Tabs.Count}",
            };
            var tabItem = new Model.UI.TabItem
            {
                IsActive = true,
                HostedWindowItem = windowItem,
            };
            windowItem.TabItem = tabItem;
            Tabs.Add(tabItem);

            ActivateTab(Tabs.Count - 1);

            AttachToConhost(process.MainWindowHandle);
        }

        private void AttachToConhost(IntPtr handle)
        {
            ContainTargetWindow(handle);
            SetWindowOpacity(handle, 0.65);
        }

        private void ActivateTab(int index)
        {
            for (int i = 0; i < Tabs.Count; i++)
                Tabs[i].IsActive = (i == index);
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

        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            switchToContentEnabled = false;
            Debug.WriteLine("MouseEnter");

            this.Activate();
        }

        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            switchToContentEnabled = true;
            Debug.WriteLine("MouseLeave");

            Win32Functions.SetForegroundWindow(CurrentContainedWindowHandle);
        }

        private void NewTab_Click(object sender, RoutedEventArgs e)
        {
            StartCmd();
        }

        private void Tab_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Tab");
            var hostedWindowItem = (sender as Control).Tag as HostedWindowItem;

            var index = Tabs.IndexOf(hostedWindowItem.TabItem);
            ActivateTab(index);

            AttachToConhost(hostedWindowItem.WindowHandle);
        }

        private void TabClose_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Tab close");

            var hostedWindowItem = (sender as Control).Tag as HostedWindowItem;

            CloseWindowProcess(hostedWindowItem.WindowHandle);

            var index = Tabs.IndexOf(hostedWindowItem.TabItem);
            var activeIndex = ActiveTabIndex;

            Tabs.RemoveAt(index);

            if (activeIndex == index)
            {
                ActivateTab(Math.Min(index + 1, Tabs.Count - 1));
            }
        }

        private void CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            foreach (var tab in Tabs)
            {
                CloseWindowProcess(tab.HostedWindowItem.WindowHandle);
            }

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
            Win32Functions.GetWindowThreadProcessId(windowHandle, out uint processId);
            var process = Process.GetProcessById((int)processId);

            // TODO: Kill child processes as well (When cmd runs another cmd or bash inside itself)

            process.Kill();
        }
    }
}
