using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using TabbedShell.Helpers;
using TabbedShell.Model;
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
                threeDotContextMenu = new ContextMenu();
                threeDotContextMenu.Items.Add(new ContextMenuClickableItem
                {
                    Text = "New Window",
                    Action = (e) =>
                    {
                        (new MainWindow()).Show();
                    }
                });
                threeDotContextMenu.Items.Add(new ContextMenuClickableItem
                {
                    Text = "Settings",
                    Action = (e) =>
                    {
                        var ownerWindow = e as MainWindow;
                        var dpi = VisualTreeHelper.GetDpi(ownerWindow);
                        SettingsWindow.ShowSettingsWindow(new Point(dpi.DpiScaleX * (ownerWindow.Left + ownerWindow.Width / 2), 
                            dpi.DpiScaleY * (ownerWindow.Top + ownerWindow.Height / 2)));
                    }
                });

                return threeDotContextMenu;
            }
        }

        public static ContextMenu NewTabContextMenu
        {
            get
            {
                newTabContextMenu = new ContextMenu
                {
                    Width = 200,
                };

                newTabContextMenu.Items.Add(new ContextMenuClickableItem
                {
                    Text = "Command Prompt",
                    Action = (ownerWindow) =>
                    {
                        (ownerWindow as MainWindow).StartProcess("cmd.exe");
                    }
                });
                newTabContextMenu.Items.Add(new ContextMenuClickableItem
                {
                    Text = "Windows PowerShell",
                    Action = (ownerWindow) =>
                    {
                        (ownerWindow as MainWindow).StartProcess("powershell.exe");
                    }
                });
                newTabContextMenu.Items.Add(new ContextMenuClickableItem
                {
                    Text = "Windows Subsystem for Linux",
                    Action = (ownerWindow) =>
                    {
                        (ownerWindow as MainWindow).StartProcess("bash.exe");
                    }
                });

                return newTabContextMenu;
            }
        }

        public static ContextMenu TabContextMenu
        {
            get
            {
                newTabContextMenu = new ContextMenu
                {
                    Width = 150,
                };

                newTabContextMenu.Items.Add(new ContextMenuExpandableItem
                {
                    Text = "Test",
                    Items = (new ContextMenuItem[]
                    {
                        new ContextMenuClickableItem
                        {
                            Text = "Item 1",
                        },
                        new ContextMenuClickableItem
                        {
                            Text = "Item 2",
                        },
                        new ContextMenuClickableItem
                        {
                            Text = "Item 3",
                        },
                        new ContextMenuClickableItem
                        {
                            Text = "Item 4",
                        },
                        new ContextMenuClickableItem
                        {
                            Text = "Item 5",
                        },
                        new ContextMenuClickableItem
                        {
                            Text = "Item 6",
                        },
                    }).ToList(),
                    ChildMenuWidth = 100,
                });
                newTabContextMenu.Items.Add(new ContextMenuExpandableItem
                {
                    Text = "Test 2",
                    Items = (new ContextMenuItem[]
                    {
                        new ContextMenuClickableItem
                        {
                            Text = "Item 1x",
                        },
                        new ContextMenuClickableItem
                        {
                            Text = "Item 2x",
                        },
                        new ContextMenuClickableItem
                        {
                            Text = "Item 3x",
                        },
                        new ContextMenuClickableItem
                        {
                            Text = "Item 4x",
                        },
                        new ContextMenuClickableItem
                        {
                            Text = "Item 5x",
                        },
                        new ContextMenuClickableItem
                        {
                            Text = "Item 6x",
                        },
                        new ContextMenuExpandableItem
                        {
                            Text = "Test 3",
                            Items = (new ContextMenuItem[]
                            {
                                new ContextMenuClickableItem
                                {
                                    Text = "Item 1xx",
                                },
                                new ContextMenuClickableItem
                                {
                                    Text = "Item 2xx",
                                },
                                new ContextMenuClickableItem
                                {
                                    Text = "Item 3xx",
                                },
                                new ContextMenuClickableItem
                                {
                                    Text = "Item 4xx",
                                },
                                new ContextMenuClickableItem
                                {
                                    Text = "Item 5xx",
                                },
                                new ContextMenuClickableItem
                                {
                                    Text = "Item 6xx",
                                },
                            }).ToList(),
                            ChildMenuWidth = 100,
                        },
                    }).ToList(),
                    ChildMenuWidth = 200,
                });
                newTabContextMenu.Items.Add(new ContextMenuClickableItem
                {
                    Text = "Properties",
                    Action = (e) =>
                    {
                        ConsoleFunctions.OpenProperties((e as WindowContextMenuInfo).HostedWindow);
                    }
                });
                newTabContextMenu.Items.Add(new ContextMenuClickableItem
                {
                    Text = "Find",
                    Action = (e) =>
                    {
                        ConsoleFunctions.OpenFind((e as WindowContextMenuInfo).HostedWindow);
                    }
                });
                newTabContextMenu.Items.Add(new ContextMenuClickableItem
                {
                    Text = "Close Tab",
                    Action = (e) =>
                    {
                        (e as WindowContextMenuInfo).ContainerWindow.TabsContainer.CloseTab((e as WindowContextMenuInfo).HostedWindow);
                    }
                });

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
    }

    public class WindowContextMenuInfo
    {
        public MainWindow ContainerWindow { get; set; }
        public HostedWindowItem HostedWindow { get; set; }
    }
}
