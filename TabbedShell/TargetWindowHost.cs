using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;
using TabbedShell.Win32.Interop;

namespace TabbedShell
{
    public class MyHost : HwndHost
    {
        // Good resource: https://github.com/Maximus5/ConEmu/blob/9629fa82c8a4c817f3b6faa2161a0a9eec9285c4/src/ConEmuHk/GuiAttach.cpp
        // ReplaceGuiAppWindow function
        
        private IntPtr childRef;

        public MyHost(IntPtr childRef)
        {
            this.childRef = childRef;
        }

        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            int style = Win32Functions.GetWindowLongPtr(childRef, Win32Functions.GWL_STYLE).ToInt32();

            int newStyle = style & ~(Win32Functions.WS_MAXIMIZEBOX | Win32Functions.WS_MINIMIZEBOX | Win32Functions.WS_CAPTION | Win32Functions.WS_THICKFRAME);
            newStyle |= Win32Functions.WS_CHILD;

            Win32Functions.SetWindowLongPtr(new HandleRef(this, childRef), Win32Functions.GWL_STYLE, new IntPtr(newStyle));

            Win32Functions.SetParent(childRef, hwndParent.Handle);

            return new HandleRef(this, childRef);
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
        }
    }
}
