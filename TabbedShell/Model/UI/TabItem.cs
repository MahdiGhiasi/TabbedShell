using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using TabbedShell.Controls;

namespace TabbedShell.Model.UI
{
    public class TabItem : ModelBase
    {
        public HostedWindowItem HostedWindowItem { get; set; }

        private string title;
        public string Title
        {
            get
            {
                return title;
            }
            set
            {
                if (title == value)
                    return;
                
                title = value;
                NotifyPropertyChanged(nameof(Title));
                ContainingTabHeader?.TabTitleUpdated(this);
            }
        }

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
                    return TabActiveBackColor;

                return TabDeactiveBackColor;
            }
        }

        public Brush TabActiveBackColor
        {
            get
            {
                return new SolidColorBrush(Color.FromArgb(255, 35, 35, 35));
            }
        }

        public Brush TabDeactiveBackColor
        {
            get
            {
                return new SolidColorBrush(Colors.Transparent);
            }
        }

        public TabHeader ContainingTabHeader { get; set; }
    }
}
