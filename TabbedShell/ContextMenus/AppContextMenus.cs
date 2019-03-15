using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TabbedShell.Model.ContextMenu;

namespace TabbedShell.ContextMenus
{
    public static class AppContextMenus
    {
        static ContextMenu threeDotContextMenu = null;
        static ContextMenu newTabContextMenu = null;

        public static ContextMenu GetThreeDotContextMenu(MainWindow ownerWindow)
        {
            if (threeDotContextMenu == null)
            {
                threeDotContextMenu = new ContextMenu();
                threeDotContextMenu.Items.Add(new ContextMenuItem
                {
                    Text = "Settings",
                    Action = async () =>
                    {
                        var uri = new Uri("tabbedshellmodern:settings");
                        await Windows.System.Launcher.LaunchUriAsync(uri);
                    }
                });
            }

            return threeDotContextMenu;
        }

        public static ContextMenu GetNewTabContextMenu(MainWindow ownerWindow)
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
                    Action = () =>
                    {
                        ownerWindow.StartProcess("cmd.exe", "Command Prompt");
                    }
                });
                newTabContextMenu.Items.Add(new ContextMenuItem
                {
                    Text = "Windows PowerShell",
                    Action = () =>
                    {
                        ownerWindow.StartProcess("powershell.exe", "PowerShell");
                    }
                });
                newTabContextMenu.Items.Add(new ContextMenuItem
                {
                    Text = "Windows Subsystem for Linux",
                    Action = () =>
                    {
                        ownerWindow.StartProcess("bash.exe", "Bash");
                    }
                });
            }

            return newTabContextMenu;
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
        }
    }
}
