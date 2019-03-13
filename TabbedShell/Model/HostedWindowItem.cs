using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TabbedShell.Model.UI;

namespace TabbedShell.Model
{
    public class HostedWindowItem
    {
        public IntPtr WindowHandle { get; set; }
        public string Title { get; set; }
        public TabItem TabItem { get; set; }

    }
}
