using System;
using System.Windows.Input;

namespace audition_nagurisaki
{
    public class InputManager : IDisposable
    {
        private KeyboardHook? _keyboardHook;

        public event EventHandler<string>? InputPressed;
        public event EventHandler<string>? InputReleased;

        public void Start()
        {
            _keyboardHook = new KeyboardHook();
            _keyboardHook.KeyPressed += OnKeyPressed;
            _keyboardHook.KeyReleased += OnKeyReleased;
            _keyboardHook.Start();
        }

        public void Stop()
        {
            _keyboardHook?.Stop();
        }

        private void OnKeyPressed(object? sender, Key key)
        {
            string keyString = ConvertKeyToString(key);
            InputPressed?.Invoke(this, keyString);
        }

        private void OnKeyReleased(object? sender, Key key)
        {
            string keyString = ConvertKeyToString(key);
            InputReleased?.Invoke(this, keyString);
        }

        public static string ConvertKeyToString(Key key)
        {
            return key switch
            {
                Key.Space => "Space",
                Key.Enter => "Enter",
                Key.Escape => "Escape",
                Key.Tab => "Tab",
                Key.Back => "Backspace",
                Key.Delete => "Delete",
                Key.Insert => "Insert",
                Key.Home => "Home",
                Key.End => "End",
                Key.PageUp => "PageUp",
                Key.PageDown => "PageDown",
                Key.Up => "Up",
                Key.Down => "Down",
                Key.Left => "Left",
                Key.Right => "Right",
                Key.F1 => "F1",
                Key.F2 => "F2",
                Key.F3 => "F3",
                Key.F4 => "F4",
                Key.F5 => "F5",
                Key.F6 => "F6",
                Key.F7 => "F7",
                Key.F8 => "F8",
                Key.F9 => "F9",
                Key.F10 => "F10",
                Key.F11 => "F11",
                Key.F12 => "F12",
                Key.LeftCtrl => "LCtrl",
                Key.RightCtrl => "RCtrl",
                Key.LeftAlt => "LAlt",
                Key.RightAlt => "RAlt",
                Key.LeftShift => "LShift",
                Key.RightShift => "RShift",
                Key.LWin => "LWin",
                Key.RWin => "RWin",
                _ => key.ToString()
            };
        }

        public void Dispose()
        {
            Stop();
            _keyboardHook?.Dispose();
        }
    }
}