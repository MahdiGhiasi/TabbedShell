using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TabbedShell.Helpers;

namespace TabbedShell
{
    /// <summary>
    /// Interaction logic for SettingWindow.xaml
    /// </summary>
    public partial class SettingsWindow
    {
        private static SettingsWindow instance = null;

        private SettingsWindow()
        {
            InitializeComponent();
        }

        public static bool IsOpen => instance != null;

        public static void ShowSettingsWindow()
        {
            if (instance == null)
            {
                instance = new SettingsWindow();
                instance.Show();
            }
            else
            {
                instance.Activate();
            }
        }

        private void MetroWindow_Closed(object sender, EventArgs e)
        {
            instance = null;
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.EnableAcrylicBlur();
        }
    }
}
