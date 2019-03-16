using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace TabbedShell.Model.UI
{
    public class TabItem : ModelBase
    {
        public HostedWindowItem HostedWindowItem { get; set; }
        public string Title => HostedWindowItem.Title;

        private bool exiting = false;
        public bool Exiting
        {
            get
            {
                return exiting;
            }
            set
            {
                exiting = value;
                NotifyPropertyChanged(nameof(Exiting));
            }
        }

        private bool isActive = false;
        public bool IsActive
        {
            get
            {
                return isActive;
            }
            set
            {
                isActive = value;
                NotifyPropertyChanged(nameof(IsActive));
                NotifyPropertyChanged(nameof(TabBackColor));
            }
        }


        public Brush TabBackColor
        {
            get
            {
                if (IsActive)
                    return new SolidColorBrush(Color.FromArgb(255, 35, 35, 35));

                return new SolidColorBrush(Colors.Transparent);
            }
        }
    }
}
