using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TabbedShell.Model.ContextMenu;

namespace TabbedShell.ContextMenus
{
    public static class AppContextMenus
    {
        static ContextMenu threeDotContextMenu = null;
        static ContextMenu newTabContextMenu = null;

        public static ContextMenu ThreeDotContextMenu
        {
            get
            {
                if (threeDotContextMenu == null)
                {
                    threeDotContextMenu = new ContextMenu();
                    threeDotContextMenu.Items.Add(new ContextMenuItem
                    {
                        Text = "New Window",
                        Action = (ownerWindow) =>
                        {
                            (new MainWindow()).Show();
                        }
                    });
                    threeDotContextMenu.Items.Add(new ContextMenuItem
                    {
                        Text = "Settings",
                        Action = (ownerWindow) =>
                        {
                            SettingsWindow.ShowSettingsWindow(new Point(ownerWindow.Left + ownerWindow.Width / 2, ownerWindow.Top + ownerWindow.Height / 2));
                        }
                    });
                }

                return threeDotContextMenu;
            }
        }

        public static ContextMenu NewTabContextMenu
        {
            get
            {
                if (newTabContextMenu == null)
                {
                    newTabContextMenu = new ContextMenu
                    {
                        Width = 200,
                    };

                    newTabContextMenu.Items.Add(new ContextMenuItem
                    {
                        Text = "Command Prompt",
                        Action = (ownerWindow) =>
                        {
                            ownerWindow.StartProcess("cmd.exe", "Command Prompt");
                        }
                    });
                    newTabContextMenu.Items.Add(new ContextMenuItem
                    {
                        Text = "Windows PowerShell",
                        Action = (ownerWindow) =>
                        {
                            ownerWindow.StartProcess("powershell.exe", "PowerShell");
                        }
                    });
                    newTabContextMenu.Items.Add(new ContextMenuItem
                    {
                        Text = "Windows Subsystem for Linux",
                        Action = (ownerWindow) =>
                        {
                            ownerWindow.StartProcess("bash.exe", "Bash");
                        }
                    });
                }

                return newTabContextMenu;
            }
        }

        public static bool IsAContextMenuOpen
        {
            get
            {
                var c1 = threeDotContextMenu?.IsOpened ?? false;
                var c2 = newTabContextMenu?.IsOpened ?? false;

                return c1 || c2;
            }
        }

        internal static void Close()
        {
            threeDotContextMenu?.Close();
            newTabContextMenu?.Close();

            threeDotContextMenu = null;
            newTabContextMenu = null;
        }
    }
}
