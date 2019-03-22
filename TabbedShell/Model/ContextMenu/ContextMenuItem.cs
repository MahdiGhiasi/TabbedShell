using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TabbedShell.Model.ContextMenu
{
    public abstract class ContextMenuItem
    {
        public string Text { get; set; }
        public abstract bool IsExpandable { get; }
    }

    public class ContextMenuClickableItem : ContextMenuItem
    {
        public Action<object> Action { get; set; }
        public override bool IsExpandable => false;
    }

    public class ContextMenuExpandableItem : ContextMenuItem
    {
        public IList<ContextMenuItem> Items { get; set; } = new ObservableCollection<ContextMenuItem>();
        public int ChildMenuWidth { get; set; } = 150;
        public override bool IsExpandable => true;

    }
}
