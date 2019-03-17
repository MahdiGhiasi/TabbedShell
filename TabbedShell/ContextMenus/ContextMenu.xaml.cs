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
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TabbedShell.Model.ContextMenu;

namespace TabbedShell.ContextMenus
{
    /// <summary>
    /// Interaction logic for ContextMenu.xaml
    /// </summary>
    public partial class ContextMenu : Window
    {
        public IList<ContextMenuItem> Items { get; private set; } = new ObservableCollection<ContextMenuItem>();

        public bool IsOpened
        {
            get
            {
                return Visibility == Visibility.Visible;
            }
        }

        Storyboard menuOpenStoryboard, menuCloseStoryboard;
        private Window ownerWindow;

        public ContextMenu()
        {
            InitializeComponent();

            ItemsList.ItemsSource = Items;
            (Items as ObservableCollection<ContextMenuItem>).CollectionChanged += ContextMenu_CollectionChanged;

            menuOpenStoryboard = (FindResource("MenuOpenStoryboard") as Storyboard);
            menuCloseStoryboard = (FindResource("MenuCloseStoryboard") as Storyboard);
        }

        private void ContextMenu_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            SetWindowSize();
        }

        private void SetWindowSize()
        {
            this.Height = ItemSizeReference.ActualHeight * Items.Count + 16 + 2;
        }

        private async void Window_Deactivated(object sender, EventArgs e)
        {
            HideContextMenuAnimation();
            await Task.Delay(150);
            Left = 100000;
            await Task.Delay(50);
            this.Hide();
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

        public async void ShowContextMenu(Window ownerWindow)
        {
            this.ownerWindow = ownerWindow;

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
            var item = (sender as Control).Tag as ContextMenuItem;
            item.Action?.Invoke(ownerWindow);
        }

        private void SetWindowPosition()
        {
            var point = this.PointToScreen(Mouse.GetPosition(this));

            this.Left = point.X - this.Width / 2;
            this.Top = point.Y;
        }
    }
}
