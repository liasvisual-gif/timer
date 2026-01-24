using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AutoClicker.Models
{
    public class ComboClickPoint : INotifyPropertyChanged
    {
        private string _keyName = string.Empty;
        private string _description = string.Empty;
        private int _clickDelay = 50;

        public string KeyName
        {
            get => _keyName;
            set
            {
                _keyName = value;
                OnPropertyChanged();
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                OnPropertyChanged();
            }
        }

        public int ClickDelay
        {
            get => _clickDelay;
            set
            {
                _clickDelay = value;
                OnPropertyChanged();
            }
        }

        public int HotkeyId { get; set; }
        public ObservableCollection<ClickPoint> Points { get; set; } = new ObservableCollection<ClickPoint>();

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
