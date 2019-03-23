using System;

namespace TabbedShell.Classes.DragAndDrop
{
    internal class DropSurfaceData
    {
        public Action<DropEventArgs> DropCallback { get; set; }
    }
}