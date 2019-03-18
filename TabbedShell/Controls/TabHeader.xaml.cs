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
using System.Windows.Threading;
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

        public event EventHandler<TabActivatedEventArgs> TabActivated;
        public event EventHandler<TabCloseEventArgs> TabClosing;
        public event EventHandler<TabCloseEventArgs> TabClosed;
        public event EventHandler<TabNewWindowRequestEventArgs> TabNewWindowRequested;
        public event EventHandler TabDragBegin;
        public event EventHandler TabDragEnd;

        ObservableCollection<Model.UI.TabItem> tabs { get; } = new ObservableCollection<Model.UI.TabItem>();
        SemaphoreSlim tabCloseSemaphore = new SemaphoreSlim(1, 1);
        Point? tabMouseDownPosition = null;

        static TabFloatingDragDropWindow tabFloatingDragDropWindow = null; 
        static Thread tabFloatingDragDropThread = null;

        private readonly double _minLengthForDrag = 10;
        private readonly double _tabMaxWidth = 201;

        public TabHeader()
        {
            InitializeComponent();

            TabsList.ItemsSource = tabs;
            tabs.CollectionChanged += Tabs_CollectionChanged;

            // Create tabFloatingDragDropWindow in new thread to avoid drag and drop blocking issue
            Task.Run(() =>
            {
                if (tabFloatingDragDropThread == null)
                {
                    tabFloatingDragDropThread = new Thread(new ThreadStart(TabFloatingDragDropThreadStartingPoint));
                    tabFloatingDragDropThread.SetApartmentState(ApartmentState.STA);
                    tabFloatingDragDropThread.IsBackground = true;
                    tabFloatingDragDropThread.Start();
                }
            });
        }

        private void TabFloatingDragDropThreadStartingPoint()
        {
            try
            {
                tabFloatingDragDropWindow = new TabFloatingDragDropWindow
                {
                    Opacity = 0,
                };

                tabFloatingDragDropWindow.Show();
                tabFloatingDragDropWindow.Hide();
                System.Windows.Threading.Dispatcher.Run();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("TabFloatingDragDropThread:" + ex.ToString());
            }
        }

        public void ActivateTab(Model.UI.TabItem tabItem)
        {
            ActivateTab(tabs.IndexOf(tabItem));
        }

        public void ActivateTab(int index)
        {
            if (index < 0 || index >= tabs.Count)
                return;

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

        public void TabTitleUpdated(Model.UI.TabItem tabItem)
        {
            if (tabItem.IsActive)
            {
                FindMyWindow().Title = tabItem.Title;
            }
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
            var da = new DoubleAnimation(Math.Min(_tabMaxWidth, ((this.ActualWidth - NewTab.ActualWidth) / tabsCount) - 1), TimeSpan.FromSeconds(0.2));
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

        public async void CloseTab(HostedWindowItem hostedWindowItem)
        {
            await tabCloseSemaphore.WaitAsync();

            try
            {
                var index = tabs.IndexOf(hostedWindowItem.TabItem);
                if (index == -1)
                    return;

                TabClosing?.Invoke(this, new TabCloseEventArgs
                {
                    WindowHandle = hostedWindowItem.WindowHandle,
                    RemainingTabs = tabs.Count - 1,
                });
                tabs[index].Exiting = true;
                SetTabReferenceSize(tabs.Count - 1);

                var activeIndex = ActiveTabIndex;
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
                    tabItem.DragAndDropping = true;

                    if (tabs.IndexOf(tabItem) == ActiveTabIndex)
                    {
                        if (ActiveTabIndex == tabs.Count - 1)
                            ActivateTab(tabs.Count - 2);
                        else
                            ActivateTab(ActiveTabIndex + 1);
                    }

                    tabFloatingDragDropWindow.Start(tabItem, _tabMaxWidth);

                    TabDragBegin?.Invoke(this, new EventArgs());

                    // Make sure UI updates with new tab activations, before being blocked by DoDragDrop
                    await Task.Delay(10);

                    try
                    {
                        // This function is blocking. The rest of the code run after drop event
                        DragDrop.DoDragDrop(draggedItem, draggedItem.DataContext, DragDropEffects.Move);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("DoDragDrop failed: " + ex.ToString());
                    }

                    await Task.Delay(100);
                    if (tabItem.Exiting && tabItem.DragAndDropping)
                    {
                        // Dropped outside
                        tabFloatingDragDropWindow.SetBackgroundColor();
                        tabItem.DragAndDropping = false;
                        tabs.Remove(tabItem);
                        var position = await tabFloatingDragDropWindow.StopMovement();
                        TabNewWindowRequested?.Invoke(this, new TabNewWindowRequestEventArgs
                        {
                            Tab = tabItem,
                            Position = position,
                        });

                        // Floating tab should stay there a bit more, so the window have time to initialize.
                        await Task.Delay(500);
                        tabFloatingDragDropWindow.Stop(fadeOut: true);
                    }
                    else
                    {
                        tabFloatingDragDropWindow.Stop();
                    }

                    
                    TabDragEnd?.Invoke(this, new EventArgs());
                }
            }
        }

        private void TabsList_Drop(object sender, DragEventArgs e)
        {
            e.Handled = true;

            Model.UI.TabItem droppedData = e.Data.GetData(typeof(Model.UI.TabItem)) as Model.UI.TabItem;
            Model.UI.TabItem target = ((ListBoxItem)(sender)).DataContext as Model.UI.TabItem;

            var addNext = (e.GetPosition((ListBoxItem)sender).X > ((ListBoxItem)sender).ActualWidth / 2);

            tabFloatingDragDropWindow.Stop();

            if (!droppedData.DragAndDropping)
                return;

            droppedData.DragAndDropping = false;
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

            tabFloatingDragDropWindow.Stop();

            if (droppedData == null)
                return;

            if (!droppedData.DragAndDropping)
                return;

            droppedData.DragAndDropping = false;
            droppedData.Exiting = false;

            droppedData.ContainingTabHeader.tabs.Remove(droppedData);
            tabs.Add(droppedData);

            droppedData.ContainingTabHeader = this;
            ActivateTab(droppedData);
        }

        private void TabsList_GiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            Mouse.SetCursor(Cursors.Arrow);

            e.UseDefaultCursors = false;
            e.Handled = true;
        }

        private void TabsList_QueryContinueDrag(object sender, QueryContinueDragEventArgs e)
        {
            SetTabFloatingDragDropWindowPosition();
        }

        private void SetTabFloatingDragDropWindowPosition()
        {
            if (tabFloatingDragDropWindow == null)
                return;
            if (tabFloatingDragDropWindow.Visibility != Visibility.Visible)
                return;

            tabFloatingDragDropWindow.FollowMouse();
        }

        public static void DisposeFloatingDragDropThread()
        {
            tabFloatingDragDropThread.Abort();
        }
    }

    public class TabNewWindowRequestEventArgs
    {
        public Model.UI.TabItem Tab { get; set; }
        public Point Position { get; set; }
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
