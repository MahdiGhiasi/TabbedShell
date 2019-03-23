using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using TabbedShell.Win32.Enums;
using TabbedShell.Win32.Interop;

namespace TabbedShell.Classes
{
    public static class WindowExtensions
    {
        public static void ForcePaint(this Window window)
        {
            var x = new WindowInteropHelper(window);
            Win32Functions.SendMessage(x.Handle, Win32Functions.WmPaint, IntPtr.Zero, IntPtr.Zero);
        }

        public static void BringToFront(this Window window)
        {
            // Bring window to front without activating it
            // https://stackoverflow.com/a/14211193/942659
            Win32Functions.SetWindowPos((new WindowInteropHelper(window)).Handle,
                Win32Functions.HWND_TOP, 0, 0, 0, 0, SetWindowPosFlags.IgnoreMove | SetWindowPosFlags.IgnoreResize | SetWindowPosFlags.DoNotActivate);
        }
    }
}
