using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TabbedShell.Classes;
using TabbedShell.Win32.Interop;
using TabbedShell.Win32.Structs;

namespace TabbedShell.Helpers
{
    public class CursorHelper
    {
        public static Point GetCursorPosition(Point scalingReferencePoint)
        {
            Win32Functions.GetCursorPos(out POINT position);

            var screen = System.Windows.Forms.Screen.AllScreens.FirstOrDefault(x => x.Bounds.Contains((int)scalingReferencePoint.X, (int)scalingReferencePoint.Y));
            screen.GetScaleFactors(out double scaleX, out double scaleY);

            return new Point(position.x, position.y);
        }

        public static Point GetRawCursorPosition()
        {
            Win32Functions.GetCursorPos(out POINT position);
            return new Point(position.x, position.y);
        }
    }
}
