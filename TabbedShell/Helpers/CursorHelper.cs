using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TabbedShell.Classes;

namespace TabbedShell.Helpers
{
    public class CursorHelper
    {
        public static Point GetCursorPosition()
        {
            var position = System.Windows.Forms.Cursor.Position;
            System.Windows.Forms.Screen.PrimaryScreen.GetScaleFactors(out double scaleX, out double scaleY);

            return new Point(position.X / scaleX, position.Y / scaleY);
        }
    }
}
