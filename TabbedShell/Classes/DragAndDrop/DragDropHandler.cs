using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TabbedShell.Helpers;
using TabbedShell.Win32.Interop;

namespace TabbedShell.Classes.DragAndDrop
{
    public class DragDropHandler
    {
        public IDragDropVisualProvider VisualProvider { get; }
        public bool IsDragging { get; private set; } = false;

        private Dictionary<Control, DropSurfaceData> dropSurfaceData = new Dictionary<Control, DropSurfaceData>();
        private List<Control> dropSurfaces = new List<Control>();
        private Win32.HookProc MouseHookProcedure;
        private IntPtr hMouseHook = IntPtr.Zero;
        private object dragObject = null;
        private TaskCompletionSource<DropEventArgs> dragDropTcs;

        public DragDropHandler(IDragDropVisualProvider visualProvider)
        {
            VisualProvider = visualProvider;
        }

        public void Initialize()
        {
            if (hMouseHook == IntPtr.Zero)
            {
                // Create an instance of HookProc.
                MouseHookProcedure = new Win32.HookProc(MouseHookProc);

                //install hook
                hMouseHook = Win32.SetWindowsHookEx(
                    Win32.HookType.WH_MOUSE_LL,
                    MouseHookProcedure,
                    IntPtr.Zero,
                    0);

                //If SetWindowsHookEx fails.
                if (hMouseHook == IntPtr.Zero)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    throw new Win32Exception(errorCode);
                }
            }
        }

        public void AddDropSurface(Control control, Action<DropEventArgs> callback)
        {
            control.MouseEnter += DropSurface_MouseEnter;
            control.MouseLeave += DropSurface_MouseLeave;

            dropSurfaces.Add(control);
            dropSurfaceData[control] = new DropSurfaceData
            {
                DropCallback = callback,
            };
        }

        public void RemoveDropSurface(Control control)
        {
            control.MouseEnter -= DropSurface_MouseEnter;
            control.MouseLeave -= DropSurface_MouseLeave;

            dropSurfaces.Remove(control);
        }

        public async Task<DropEventArgs> DoDragDrop(object data)
        {
            dragDropTcs = new TaskCompletionSource<DropEventArgs>();

            this.IsDragging = true;
            this.dragObject = data;

            VisualProvider?.ShowVisual(data, CursorHelper.GetRawCursorPosition());

            return await dragDropTcs.Task;
        }

        private void DropSurface_MouseLeave(object sender, MouseEventArgs e)
        {

        }

        private void DropSurface_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!IsDragging)
                return;

            FindWindowOf(sender as Control).BringToFront();
        }

        private Window FindWindowOf(FrameworkElement control)
        {
            while (!(control is Window))
            {
                control = control.Parent as FrameworkElement;
            }

            return control as Window;
        }

        private IntPtr MouseHookProc(int code, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (code >= 0 && IsDragging)
                {
                    if ((Win32.MouseMessages)wParam == Win32.MouseMessages.WM_LBUTTONDOWN)
                    {
                        Win32.MSLLHOOKSTRUCT hookStruct = (Win32.MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(Win32.MSLLHOOKSTRUCT));
                        Console.WriteLine("DOWN: " + hookStruct.pt.x + ", " + hookStruct.pt.y);
                    }
                    else if ((Win32.MouseMessages)wParam == Win32.MouseMessages.WM_MOUSEMOVE)
                    {
                        Win32.MSLLHOOKSTRUCT hookStruct = (Win32.MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(Win32.MSLLHOOKSTRUCT));
                        Console.WriteLine("MOVE: " + hookStruct.pt.x + ", " + hookStruct.pt.y);

                        VisualProvider?.UpdateVisualPosition(new Point(hookStruct.pt.x, hookStruct.pt.y));
                    }
                    else if ((Win32.MouseMessages)wParam == Win32.MouseMessages.WM_LBUTTONUP)
                    {
                        Win32.MSLLHOOKSTRUCT hookStruct = (Win32.MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(Win32.MSLLHOOKSTRUCT));
                        Console.WriteLine("UP: " + hookStruct.pt.x + ", " + hookStruct.pt.y);

                        DragRelease(new Point(hookStruct.pt.x, hookStruct.pt.y));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("MouseHookProc: " + ex.ToString());
            }

            return Win32.CallNextHookEx(hMouseHook, code, wParam, lParam);
        }

        private async Task DragRelease(Point dropPoint)
        {
            this.IsDragging = false;

            VisualProvider?.UpdateVisualPosition(dropPoint);

            // Fire relevant callback function if dropped in a dropSurface
            foreach (var dropSurface in dropSurfaces)
            {
                if (dropSurface.IsMouseOver)
                {
                    var dpi = VisualTreeHelper.GetDpi(dropSurface);
                    var scaledPoint = new Point(dropPoint.X / dpi.DpiScaleX, dropPoint.Y / dpi.DpiScaleY);
                    var dropSurfaceScreenPoint = dropSurface.PointToScreen(new Point(0, 0));
                    var relativePoint = new Point(scaledPoint.X - dropSurfaceScreenPoint.X,
                        scaledPoint.Y - dropSurfaceScreenPoint.Y);

                    var dropArgs = new DropEventArgs
                    {
                        Result = DragDropResult.DroppedToExistingWindow,
                        Data = this.dragObject,
                        RelativeMousePosition = relativePoint,
                        DropSurface = dropSurface,
                    };

                    dropSurfaceData[dropSurface].DropCallback(dropArgs);
                    VisualProvider?.CloseVisual(dropArgs);
                    dragDropTcs.SetResult(dropArgs);
                    return;
                }
            }

            // Fire event if dropped was not in any dropSurface

            var newWindowDropArgs = new DropEventArgs
            {
                Result = DragDropResult.NewWindowRequested,
                Data = this.dragObject,
                DropSurface = null,
                RelativeMousePosition = dropPoint,
            };

            if (VisualProvider != null)
            {
                var newWindowPosition = await VisualProvider.CloseVisual(newWindowDropArgs);
                newWindowDropArgs.RelativeMousePosition = newWindowPosition;
            }

            dragDropTcs.SetResult(newWindowDropArgs);
        }
    }
}
