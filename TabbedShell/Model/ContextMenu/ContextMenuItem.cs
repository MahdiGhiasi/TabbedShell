using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TabbedShell.Model.ContextMenu
{
    public class ContextMenuItem
    {
        public string Text { get; set; }
        public Action<MainWindow> Action { get; set; }
    }
}
