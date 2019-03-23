using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TabbedShell.Model;
using TabbedShell.Win32.Interop;
using TabbedShell.Win32.Structs;

namespace TabbedShell.Helpers
{
    public static class ConsoleFunctions
    {
        const int SC_PROPERTIES_SECRET = 0x0000fff7;
        const int SC_MARK_SECRET = 0x0000fff2;
        const int SC_COPY_ENTER_SECRET = 0x0000fff0;
        const int SC_PASTE_SECRET = 0x0000fff1;
        const int SC_SELECTALL_SECRET = 0x0000fff5;
        const int SC_SCROLL_SECRET = 0x0000fff3;
        const int SC_FIND_SECRET = 0x0000fff4;

        public static void OpenProperties(HostedWindowItem console)
        {
            Win32Functions.PostMessage(console.WindowHandle, Win32Functions.WM_SYSCOMMAND, new IntPtr(SC_PROPERTIES_SECRET), IntPtr.Zero);
        }

        public static void OpenFind(HostedWindowItem console)
        {
            Win32Functions.PostMessage(console.WindowHandle, Win32Functions.WM_SYSCOMMAND, new IntPtr(SC_FIND_SECRET), IntPtr.Zero);
        }

        public static void SendDpiChangedMessage(IntPtr windowHandle, int dpi)
        {
            if (Win32Functions.GetWindowRect(windowHandle, out RECT rect)) {
                var res = Win32Functions.SendMessage(windowHandle, Win32Functions.WM_DPICHANGED, new IntPtr(dpi | (dpi << 16)), rect);
            }
        }
    }
}
