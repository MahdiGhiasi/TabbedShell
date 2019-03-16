using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TabbedShell.Model.UI
{
    public class SettingsModel : ModelBase
    {
        public bool AttachToAllTerminalsEnabled
        {
            get
            {
                return Properties.Settings.Default.AttachToAllTerminalsEnabled;
            }
            set
            {
                Properties.Settings.Default.AttachToAllTerminalsEnabled = value;
                Properties.Settings.Default.Save();

                NotifyPropertyChanged(nameof(AttachToAllTerminalsEnabled));
            }
        }

        public bool TransparentTerminalEnabled
        {
            get
            {
                return Properties.Settings.Default.TransparentTerminalEnabled;
            }
            set
            {
                Properties.Settings.Default.TransparentTerminalEnabled = value;
                Properties.Settings.Default.Save();

                NotifyPropertyChanged(nameof(TransparentTerminalEnabled));
                NotifyPropertyChanged(nameof(TransparentTerminalSectionOpacity));
                NotifyOpacitySettingChangedToMainWindows();
            }
        }

        public double TerminalTransparencyAmount
        {
            get
            {
                return Properties.Settings.Default.TerminalTransparencyAmount * 100;
            }
            set
            {
                Properties.Settings.Default.TerminalTransparencyAmount = value / 100.0;
                Properties.Settings.Default.Save();

                NotifyPropertyChanged(nameof(TerminalTransparencyAmount));
                NotifyOpacitySettingChangedToMainWindows();
            }
        }

        private static void NotifyOpacitySettingChangedToMainWindows()
        {
            foreach (var item in (App.Current as App).MainWindows)
                item.WindowOpacityUpdated();
        }

        public double TransparentTerminalSectionOpacity
        {
            get
            {
                return TransparentTerminalEnabled ? 1.0 : 0.5;
            }
        }

        public int NewTabBehaviorSelectedIndex
        {
            get
            {
                return Properties.Settings.Default.NewTabDefaultIndex;
            }
            set
            {
                Properties.Settings.Default.NewTabDefaultIndex = value;
                Properties.Settings.Default.Save();

                NotifyPropertyChanged(nameof(NewTabBehaviorSelectedIndex));
            }
        }
    }
}
