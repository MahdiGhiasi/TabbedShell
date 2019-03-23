using System.Windows;
using System.Windows.Controls;

namespace TabbedShell.Classes.DragAndDrop
{
    public class DropEventArgs
    {
        public object Data { get; set; }
        public Point RelativeMousePosition { get; set; }
        public Control DropSurface { get; set; }
        public DragDropResult Result { get; set; }
    }
}