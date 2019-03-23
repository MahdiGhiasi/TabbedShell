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
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using TabbedShell.Helpers;
using TabbedShell.Model.UI;
using TabbedShell.Win32.Interop;

namespace TabbedShell
{
    /// <summary>
    /// Interaction logic for TabFloatingDragDropWindow.xaml
    /// </summary>
    public partial class TabFloatingDragDropWindow : Window
    {
        private Model.UI.TabItem tabItem;

        public TabFloatingDragDropWindow()
        {
            InitializeComponent();
        }

        public void FollowMouse()
        {
            FollowMouse(CursorHelper.GetRawCursorPosition());
        }

        public void FollowMouse(Point cursorPosition)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
            {
                var dpi = VisualTreeHelper.GetDpi(this);

                var left = cursorPosition.X - 4 * dpi.DpiScaleX;
                var top = cursorPosition.Y - 15 * dpi.DpiScaleY;

                this.Left = left / dpi.DpiScaleX;
                this.Top = top / dpi.DpiScaleY;
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

                    this.Show();
                }));
            }
            catch { }
        }

        public void Stop(bool fadeOut = false)
        {
            try
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(async () =>
                {
                    if (fadeOut)
                    {
                        var animationDuration = TimeSpan.FromSeconds(0.2);
                        var animation = new DoubleAnimation(0, animationDuration)
                        {
                            EasingFunction = new ExponentialEase
                            {
                                EasingMode = EasingMode.EaseOut,
                            },
                            FillBehavior = FillBehavior.Stop,
                        };
                        this.BeginAnimation(Window.OpacityProperty, animation);
                        await Task.Delay(animationDuration.Add(TimeSpan.FromMilliseconds(-10)));
                        this.Hide();
                    }
                    else
                    {
                        this.Hide();
                    }
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
                    var dpi = VisualTreeHelper.GetDpi(this);
                    tcs.TrySetResult(new Point(this.Left * dpi.DpiScaleX, this.Top * dpi.DpiScaleY));
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
