using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TabbedShell.Model.ContextMenu
{
    public class ContextMenuItem
    {
        public string Text { get; set; }
        public Action<Window> Action { get; set; }
    }
}
