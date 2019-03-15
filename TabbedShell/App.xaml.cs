using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace TabbedShell
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            if (e.Args.Length > 0)
            {
                if (e.Args[0].StartsWith("tabbedshell:"))
                {
                    var activationUrl = e.Args[0];


                }
            }
            
            base.OnStartup(e);
        }
    }
}
