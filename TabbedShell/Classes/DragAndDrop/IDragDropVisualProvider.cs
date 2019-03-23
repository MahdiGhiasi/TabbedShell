using System.Threading.Tasks;
using System.Windows;

namespace TabbedShell.Classes.DragAndDrop
{
    public interface IDragDropVisualProvider
    {
        void ShowVisual(object data, Point cursorPosition);
        void UpdateVisualPosition(Point cursorPosition);
        Task<Point> CloseVisual(DropEventArgs e);
    }
}