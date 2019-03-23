using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TabbedShell.Classes.DragAndDrop
{
    public class TabItemDragDropVisualProvider : IDragDropVisualProvider
    {
        private TabFloatingDragDropWindow tabFloatingDragDropWindow;

        public async Task<Point> CloseVisual(DropEventArgs e)
        {
            if (e.DropSurface == null)
            {
                // Dropped outside
                tabFloatingDragDropWindow.SetBackgroundColor();
                var location = await tabFloatingDragDropWindow.StopMovement();

                // Floating tab should stay there a bit more, so the window have time to initialize.
                FadeOutAfterDelay();

                return location;
            }
            else
            {
                var location = await tabFloatingDragDropWindow.StopMovement();
                tabFloatingDragDropWindow.Stop();

                return location;
            }
        }

        private async void FadeOutAfterDelay()
        {
            await Task.Delay(500);
            tabFloatingDragDropWindow.Stop(fadeOut: true);
        }

        public void ShowVisual(object data, Point cursorPosition)
        {
            if (tabFloatingDragDropWindow == null)
            {
                tabFloatingDragDropWindow = new TabFloatingDragDropWindow
                {
                    Opacity = 0,
                };
            }

            tabFloatingDragDropWindow.Start(data as Model.UI.TabItem, 201);
        }

        public void UpdateVisualPosition(Point cursorPosition)
        {
            tabFloatingDragDropWindow.FollowMouse(cursorPosition);
        }
    }
}
