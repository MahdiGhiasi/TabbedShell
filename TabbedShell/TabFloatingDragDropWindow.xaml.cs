using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using TabbedShell.Model.UI;

namespace TabbedShell
{
    /// <summary>
    /// Interaction logic for TabFloatingDragDropWindow.xaml
    /// </summary>
    public partial class TabFloatingDragDropWindow : Window
    {
        private Model.UI.TabItem tabItem;

        private DispatcherTimer timer;

        public TabFloatingDragDropWindow()
        {
            InitializeComponent();

            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1),
            };
            timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            FollowMouse();
        }

        public void FollowMouse()
        {
            if (!timer.IsEnabled)
                return;

            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
            {
                var point = System.Windows.Forms.Cursor.Position;

                this.Left = point.X - 4;
                this.Top = point.Y - 15;
            }));
        }

        public void Start(Model.UI.TabItem tabItem, double width)
        {
            try
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
                {
                    this.tabItem = tabItem;
                    this.Background = new SolidColorBrush(Colors.Transparent);
                    this.tabTitle.Content = tabItem.Title;
                    this.Width = width;
                    this.Opacity = 1;
                    MainBorder.BorderThickness = new Thickness(0.5);
                    ColoredBorder.BorderThickness = new Thickness(0.5);
                    ColoredBorder.BorderBrush = tabItem.TabActiveBackColor;

                    timer.Start();
                    this.Show();
                }));
            }
            catch { }
        }

        public void Stop()
        {
            try
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
                {
                    timer.Stop();
                    this.Hide();
                }));
            }
            catch { }
        }

        internal void StopAndClose()
        {
            try
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
                {
                    timer.Stop();
                    this.Close();
                }));
            }
            catch { }
        }

        internal async Task<Point> StopMovement()
        {
            try
            {
                TaskCompletionSource<Point> tcs = new TaskCompletionSource<Point>();

                await Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
                {
                    timer.Stop();
                    tcs.TrySetResult(new Point(this.Left, this.Top));
                }));

                return await tcs.Task;
            }
            catch { }

            return new Point(0, 0);
        }

        public void SetBackgroundColor()
        {
            try
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
                {
                    this.Background = tabItem.TabActiveBackColor;
                    ColoredBorder.BorderThickness = new Thickness(0);
                    MainBorder.BorderThickness = new Thickness(0);
                }));
            }
            catch { }
        }
    }
}
