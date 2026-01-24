using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AutoClicker.Models
{
    public class ClickPoint : INotifyPropertyChanged
    {
        private int _x;
        private int _y;
        private string _keyName = string.Empty;
        private string _description = string.Empty;
        private int _clickDelay = 0; // í‚É0ŒÅ’è
        private bool _isCombo = false;
        private int _rapidClickInterval = 100;
        private bool _isRapidClickActive = false;
        private CancellationTokenSource? _rapidClickCancellation = null;
        private bool _useCtrl = false;
        private bool _useShift = false;
        private bool _useAlt = false;
        private string _deviceType = "Keyboard"; // Keyboard, Joystick
        private int _joyButtonNumber = 0;

        public int X
        {
            get => _x;
            set
            {
                _x = value;
                OnPropertyChanged();
            }
        }

        public int Y
        {
            get => _y;
            set
            {
                _y = value;
                OnPropertyChanged();
            }
        }

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

        public bool IsCombo
        {
            get => _isCombo;
            set
            {
                _isCombo = value;
                OnPropertyChanged();
            }
        }

        public int RapidClickInterval
        {
            get => _rapidClickInterval;
            set
            {
                _rapidClickInterval = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RapidSpeed));
            }
        }

        public double RapidSpeed
        {
            get => _rapidClickInterval > 0 ? Math.Round(1000.0 / _rapidClickInterval, 1) : 0;
            set
            {
                if (value > 0)
                {
                    _rapidClickInterval = (int)Math.Round(1000.0 / value);
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(RapidClickInterval));
                }
            }
        }

        public bool IsRapidClickActive
        {
            get => _isRapidClickActive;
            set
            {
                _isRapidClickActive = value;
                OnPropertyChanged();
            }
        }

        public bool UseCtrl
        {
            get => _useCtrl;
            set
            {
                _useCtrl = value;
                OnPropertyChanged();
            }
        }

        public bool UseShift
        {
            get => _useShift;
            set
            {
                _useShift = value;
                OnPropertyChanged();
            }
        }

        public bool UseAlt
        {
            get => _useAlt;
            set
            {
                _useAlt = value;
                OnPropertyChanged();
            }
        }

        public string DeviceType
        {
            get => _deviceType;
            set
            {
                _deviceType = value;
                OnPropertyChanged();
            }
        }

        public int JoyButtonNumber
        {
            get => _joyButtonNumber;
            set
            {
                _joyButtonNumber = value;
                OnPropertyChanged();
            }
        }

        public CancellationTokenSource? RapidClickCancellation
        {
            get => _rapidClickCancellation;
            set => _rapidClickCancellation = value;
        }

        public int HotkeyId { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
