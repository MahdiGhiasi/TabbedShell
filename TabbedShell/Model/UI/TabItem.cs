using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace TabbedShell.Model.UI
{
    public class TabItem : INotifyPropertyChanged
    {
        public HostedWindowItem HostedWindowItem { get; set; }
        public string Title => HostedWindowItem.Title;

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

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
