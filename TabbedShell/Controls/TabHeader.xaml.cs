using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
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
using TabbedShell.ContextMenus;
using TabbedShell.Model;
using TabbedShell.Model.UI;

namespace TabbedShell.Controls
{
    /// <summary>
    /// Interaction logic for TabHeader.xaml
    /// </summary>
    public partial class TabHeader : UserControl
    {
        public IReadOnlyList<Model.UI.TabItem> Tabs => tabs;
        public int ActiveTabIndex => tabs.IndexOf(tabs.FirstOrDefault(x => x.IsActive));
        public IntPtr CurrentContainedWindowHandle => ActiveTabIndex == -1 ? IntPtr.Zero : tabs[ActiveTabIndex].HostedWindowItem.WindowHandle;

        public event EventHandler<TabActivatedEventArgs> TabActivated;
        public event EventHandler<TabCloseEventArgs> TabClosing;
        public event EventHandler<TabCloseEventArgs> TabClosed;

        private ObservableCollection<Model.UI.TabItem> tabs { get; } = new ObservableCollection<Model.UI.TabItem>();
        SemaphoreSlim tabCloseSemaphore = new SemaphoreSlim(1, 1);
        Point? tabMouseDownPosition = null;

        private readonly double _minLengthForDrag = 10;

        public TabHeader()
        {
            InitializeComponent();

            TabsList.ItemsSource = tabs;
            tabs.CollectionChanged += Tabs_CollectionChanged;
        }

        public void ActivateTab(Model.UI.TabItem tabItem)
        {
            ActivateTab(tabs.IndexOf(tabItem));
        }

        public void ActivateTab(int index)
        {
            for (int i = 0; i < tabs.Count; i++)
                tabs[i].IsActive = (i == index);

            TabActivated?.Invoke(this, new TabActivatedEventArgs
            {
                WindowHandle = tabs[index].HostedWindowItem.WindowHandle,
            });
        }

        public void AddTab(Model.UI.TabItem tabItem)
        {
            tabItem.ContainingTabHeader = this;
            tabs.Add(tabItem);
            ActivateTab(tabs.Count - 1);
        }

        private Window FindMyWindow()
        {
            FrameworkElement control = this;
            while (!(control is Window))
            {
                control = control.Parent as FrameworkElement;
            }

            return control as Window;
        }

        private void Tabs_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            SetTabReferenceSize();
        }

        private void SetTabReferenceSize()
        {
            SetTabReferenceSize(tabs.Count);
        }

        private void SetTabReferenceSize(int tabsCount)
        {
            var da = new DoubleAnimation(Math.Min(201, ((this.ActualWidth - NewTab.ActualWidth) / tabsCount) - 1), TimeSpan.FromSeconds(0.2));
            da.EasingFunction = new ExponentialEase();
            TabReferenceSize.BeginAnimation(Grid.WidthProperty, da);
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetTabReferenceSize();
        }

        private void NewTab_Click(object sender, RoutedEventArgs e)
        {
            if (Properties.Settings.Default.NewTabDefaultIndex == 0)
                AppContextMenus.NewTabContextMenu.ShowContextMenu(FindMyWindow());
            else
                AppContextMenus.NewTabContextMenu.Items[Properties.Settings.Default.NewTabDefaultIndex - 1].Action?.Invoke(FindMyWindow());
        }

        private void Tab_Click(object sender, RoutedEventArgs e)
        {
            var hostedWindowItem = (sender as Control).Tag as HostedWindowItem;

            var index = tabs.IndexOf(hostedWindowItem.TabItem);
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
                TabClosing?.Invoke(this, new TabCloseEventArgs
                {
                    WindowHandle = hostedWindowItem.WindowHandle,
                    RemainingTabs = tabs.Count - 1,
                });

                var index = tabs.IndexOf(hostedWindowItem.TabItem);
                var activeIndex = ActiveTabIndex;

                tabs[index].Exiting = true;

                SetTabReferenceSize(tabs.Count - 1);

                if (tabs.Count > 1 && activeIndex == index)
                {
                    if (index == tabs.Count - 1)
                        ActivateTab(tabs.Count - 2);
                    else
                        ActivateTab(index + 1);
                }

                await Task.Delay(200);
                tabs.RemoveAt(index);

                TabClosed?.Invoke(this, new TabCloseEventArgs
                {
                    WindowHandle = hostedWindowItem.WindowHandle,
                    RemainingTabs = tabs.Count,
                });
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

        private void TabsList_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            tabMouseDownPosition = e.GetPosition(FindMyWindow());
        }

        private void TabsList_MouseUp(object sender, MouseButtonEventArgs e)
        {
            tabMouseDownPosition = null;
        }

        private async void TabsList_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (sender is ListBoxItem && e.LeftButton == MouseButtonState.Pressed)
            {
                if (tabMouseDownPosition.HasValue && Point.Subtract(e.GetPosition(FindMyWindow()), tabMouseDownPosition.Value).Length > _minLengthForDrag)
                {
                    var draggedItem = sender as ListBoxItem;
                    var tabItem = draggedItem.Content as Model.UI.TabItem;

                    if (tabItem == null)
                        return;

                    tabItem.Exiting = true;

                    if (tabs.IndexOf(tabItem) == ActiveTabIndex)
                    {
                        if (ActiveTabIndex == tabs.Count - 1)
                            ActivateTab(tabs.Count - 2);
                        else
                            ActivateTab(ActiveTabIndex + 1);
                    }

                    // Make sure UI updates with new tab activations, before being blocked by DoDragDrop
                    await Task.Delay(10); 
                    
                    // This function is blocking. The rest of the code run after drop event
                    DragDrop.DoDragDrop(draggedItem, draggedItem.DataContext, DragDropEffects.Move);


                    // TODO: When dropped outside, tab disappears. (Exiting remains true)
                }
            }
        }

        private void TabsList_Drop(object sender, DragEventArgs e)
        {
            e.Handled = true;

            Model.UI.TabItem droppedData = e.Data.GetData(typeof(Model.UI.TabItem)) as Model.UI.TabItem;
            Model.UI.TabItem target = ((ListBoxItem)(sender)).DataContext as Model.UI.TabItem;

            var addNext = (e.GetPosition((ListBoxItem)sender).X > ((ListBoxItem)sender).ActualWidth / 2);

            droppedData.Exiting = false;

            if (addNext)
            {
                droppedData.ContainingTabHeader.tabs.Remove(droppedData);
                tabs.Insert(tabs.IndexOf(target) + 1, droppedData);
            }
            else
            {
                droppedData.ContainingTabHeader.tabs.Remove(droppedData);
                tabs.Insert(tabs.IndexOf(target), droppedData);
            }

            droppedData.ContainingTabHeader = this;
            ActivateTab(droppedData);
        }

        private void UserControl_Drop(object sender, DragEventArgs e)
        {
            Model.UI.TabItem droppedData = e.Data.GetData(typeof(Model.UI.TabItem)) as Model.UI.TabItem;

            if (droppedData == null)
                return;

            droppedData.Exiting = false;

            droppedData.ContainingTabHeader.tabs.Remove(droppedData);
            tabs.Add(droppedData);

            droppedData.ContainingTabHeader = this;
            ActivateTab(droppedData);
        }
    }

    public class TabCloseEventArgs
    {
        public IntPtr WindowHandle { get; set; }
        public int RemainingTabs { get; set; }
    }

    public class TabActivatedEventArgs : EventArgs
    {
        public IntPtr WindowHandle { get; set; }
    }
}
