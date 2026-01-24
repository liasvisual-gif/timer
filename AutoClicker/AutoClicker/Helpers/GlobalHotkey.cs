using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace AutoClicker.Helpers
{
    public class GlobalHotkey : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int WM_HOTKEY = 0x0312;

        public const uint MOD_NONE = 0x0000;
        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN = 0x0008;

        private IntPtr _windowHandle;
        private Dictionary<int, Action> _hotkeyActions = new Dictionary<int, Action>();

        public void Initialize(IntPtr windowHandle, HwndSource source)
        {
            _windowHandle = windowHandle;
            source.AddHook(HwndHook);
        }

        public bool Register(int id, uint modifiers, uint key, Action action)
        {
            if (_hotkeyActions.ContainsKey(id))
            {
                Unregister(id);
            }

            bool success = RegisterHotKey(_windowHandle, id, modifiers, key);
            if (success)
            {
                _hotkeyActions[id] = action;
            }
            return success;
        }

        public void Unregister(int id)
        {
            if (_hotkeyActions.ContainsKey(id))
            {
                UnregisterHotKey(_windowHandle, id);
                _hotkeyActions.Remove(id);
            }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (_hotkeyActions.ContainsKey(id))
                {
                    _hotkeyActions[id]?.Invoke();
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            foreach (var id in _hotkeyActions.Keys.ToList())
            {
                Unregister(id);
            }
            _hotkeyActions.Clear();
        }
    }
}
