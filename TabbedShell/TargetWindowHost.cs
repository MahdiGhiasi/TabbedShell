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
            int style = WindowFunctions.GetWindowLongPtr(childRef, WindowFunctions.GWL_STYLE).ToInt32();

            int newStyle = style & ~(WindowFunctions.WS_MAXIMIZEBOX | WindowFunctions.WS_MINIMIZEBOX | WindowFunctions.WS_CAPTION | WindowFunctions.WS_THICKFRAME);
            newStyle |= WindowFunctions.WS_CHILD;

            WindowFunctions.SetWindowLongPtr(new HandleRef(this, childRef), WindowFunctions.GWL_STYLE, new IntPtr(newStyle));

            WindowFunctions.SetParent(childRef, hwndParent.Handle);

            return new HandleRef(this, childRef);
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
        }
    }
}
