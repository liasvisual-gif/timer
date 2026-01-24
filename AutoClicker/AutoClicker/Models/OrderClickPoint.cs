using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AutoClicker.Models
{
    public class OrderClickPoint : INotifyPropertyChanged
    {
        private string _keyName = string.Empty;
        private string _description = string.Empty;

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

        public int HotkeyId { get; set; }
        public ObservableCollection<int> ClickPointIds { get; set; } = new ObservableCollection<int>();

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
