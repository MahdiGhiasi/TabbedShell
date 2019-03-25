using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
using System.Windows.Shapes;
using TabbedShell.Helpers;
using TabbedShell.Model.ContextMenu;
using TabbedShell.Win32.Interop;

namespace TabbedShell.ContextMenus
{
    /// <summary>
    /// Interaction logic for ContextMenu.xaml
    /// </summary>
    public partial class ContextMenu : Window
    {
        public IList<ContextMenuItem> Items { get; private set; } = new ObservableCollection<ContextMenuItem>();

        public bool IsOpened { get; private set; }

        public event EventHandler MenuClosing;

        private readonly TimeSpan subMenuOpenMouseOverDelay = TimeSpan.FromSeconds(0.3);
        private readonly TimeSpan subMenuCloseMouseOverDelay = TimeSpan.FromSeconds(0.3);

        Storyboard menuOpenStoryboard, menuCloseStoryboard;
        private Window ownerWindow;
        private Func<object> invokeObjectCreator;
        private Point windowTopCenterPosition;
        private bool hasChildMenuOpen = false;
        private ContextMenu childMenu;

        public ContextMenu()
            : this(CursorHelper.GetRawCursorPosition())
        {
        }

        public ContextMenu(Point position)
        {
            InitializeComponent();

            ItemsList.ItemsSource = Items;
            (Items as ObservableCollection<ContextMenuItem>).CollectionChanged += ContextMenu_CollectionChanged;

            menuOpenStoryboard = (FindResource("MenuOpenStoryboard") as Storyboard);
            menuCloseStoryboard = (FindResource("MenuCloseStoryboard") as Storyboard);

            windowTopCenterPosition = position;
        }

        private void ContextMenu_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            SetWindowSize();
        }

        private void SetWindowSize()
        {
            this.Height = ItemSizeReference.ActualHeight * Items.Count + 16 + 2;
            MainGrid.Height = this.Height - 2;
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            if (!hasChildMenuOpen)
                CloseContextMenu();
        }

        private async void CloseContextMenu()
        {
            MenuClosing?.Invoke(this, new EventArgs());
            HideContextMenuAnimation();
            await Task.Delay(150);
            Left = 100000;
            await Task.Delay(50);
            IsOpened = false;
            this.Close();
        }

        private void HideContextMenuAnimation()
        {
            menuOpenStoryboard.Stop();
            (menuCloseStoryboard.Children[0] as DoubleAnimation).From = this.Height;
            menuCloseStoryboard.Begin();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SetWindowSize();
            SetWindowPosition();
        }

        public async void ShowContextMenu(Window ownerWindow, Func<object> invokeObjectCreator)
        {
            this.ownerWindow = ownerWindow;
            this.invokeObjectCreator = invokeObjectCreator;
            IsOpened = true;

            this.Opacity = 0;
            this.Show();
            SetWindowSize();
            ShowContextMenuAnimation();

            await Task.Delay(50);

            SetWindowPosition();
            this.Opacity = 1;
        }

        private void ShowContextMenuAnimation()
        {
            (menuOpenStoryboard.Children[0] as DoubleAnimation).To = this.Height;
            menuOpenStoryboard.Begin();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Control).Tag is ContextMenuClickableItem)
            {
                var item = (sender as Control).Tag as ContextMenuClickableItem;
                item?.Action?.Invoke(invokeObjectCreator());
            }
            else if ((sender as Control).Tag is ContextMenuExpandableItem)
            {
                var item = sender as Control;

                OpenChildMenu(sender as Control);
            }
        }

        private void OpenChildMenu(Control menuItem)
        {
            var contextMenuItem = menuItem.Tag as ContextMenuExpandableItem;
            var dpi = VisualTreeHelper.GetDpi(this);

            if (hasChildMenuOpen && childMenu != null && childMenu.Tag == contextMenuItem)
                return;

            childMenu = new ContextMenu(new Point(windowTopCenterPosition.X + dpi.DpiScaleX * this.Width / 2 + dpi.DpiScaleX * contextMenuItem.ChildMenuWidth / 2,
                windowTopCenterPosition.Y + dpi.DpiScaleX * menuItem.TransformToAncestor(this).Transform(new Point(0, 0)).Y))
            {
                Width = contextMenuItem.ChildMenuWidth,
                Tag = contextMenuItem,
            };
            foreach (var item in contextMenuItem.Items)
                childMenu.Items.Add(item);

            childMenu.MenuClosing += (s, e) =>
            {
                hasChildMenuOpen = false;
                if (!this.IsMouseOver)
                {
                    CloseContextMenu();
                }
            };

            hasChildMenuOpen = true;
            childMenu.ShowContextMenu(ownerWindow, invokeObjectCreator);

            // TODO: Don't hide and reshow if user clicks on the item that is already expanded.
        }

        private async void MenuItem_MouseEnter(object sender, MouseEventArgs e)
        {
            if ((sender as Control).Tag is ContextMenuExpandableItem)
            {
                await Task.Delay(subMenuOpenMouseOverDelay);

                if ((sender as Control).IsMouseOver)
                    OpenChildMenu(sender as Control);
            }
            else if ((sender as Control).Tag is ContextMenuClickableItem)
            {
                await Task.Delay(subMenuCloseMouseOverDelay);

                if ((sender as Control).IsMouseOver)
                    this.Activate(); // Close any child menus that are open
            }
        }

        private void SetWindowPosition()
        {
            var dpi = VisualTreeHelper.GetDpi(this);

            var left = windowTopCenterPosition.X - dpi.DpiScaleX * this.Width / 2;
            var top = windowTopCenterPosition.Y;

            this.Left = left / dpi.DpiScaleX;
            this.Top = top / dpi.DpiScaleY;
        }
    }
}
