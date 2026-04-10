using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace laptimer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private const long PerLapValue = 2_300_000;
    private const long TargetSpeed = 18_648_649;

    private static readonly SolidColorBrush SpeedBrushAhead = new(Color.FromRgb(0x89, 0xB4, 0xFA));
    private static readonly SolidColorBrush SpeedBrushSlightlyBehind = new(Color.FromRgb(0xF9, 0xE2, 0xAF));
    private static readonly SolidColorBrush SpeedBrushBehind = new(Color.FromRgb(0xF3, 0x8B, 0xA8));
    private static readonly SolidColorBrush BorderBrushNormal = new(Color.FromRgb(0x58, 0x5B, 0x70));
    private static readonly SolidColorBrush BorderBrushActive = new(Color.FromRgb(0xCB, 0xA6, 0xF7));

    private static readonly string SaveFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "laptimer", "state.json");

    #region Low-level keyboard hook

    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int VK_CONTROL = 0x11;
    private const int VK_SHIFT = 0x10;
    private const int VK_MENU = 0x12;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    private readonly LowLevelKeyboardProc _hookCallback;
    private IntPtr _hookId = IntPtr.Zero;

    #endregion

    private readonly Stopwatch _totalStopwatch = new();
    private readonly Stopwatch _lapStopwatch = new();
    private readonly DispatcherTimer _displayTimer;

    private TimeSpan _totalOffset;
    private TimeSpan _lapOffset;

    private Key _registeredKey = Key.Space;
    private ModifierKeys _registeredModifiers = ModifierKeys.None;
    private bool _isCapturingKey;

    private int _lapCount;
    private bool _isRunning;
    private bool _isStopped;

    private TimeSpan TotalElapsed => _totalOffset + _totalStopwatch.Elapsed;
    private TimeSpan LapElapsed => _lapOffset + _lapStopwatch.Elapsed;

    public MainWindow()
    {
        InitializeComponent();

        _displayTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _displayTimer.Tick += DisplayTimer_Tick;

        _hookCallback = HookCallback;
        Loaded += (_, _) =>
        {
            using var process = Process.GetCurrentProcess();
            using var module = process.MainModule!;
            _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _hookCallback, GetModuleHandle(module.ModuleName), 0);
            LoadState();
        };
        Closing += (_, _) => SaveState();
        Closed += (_, _) =>
        {
            if (_hookId != IntPtr.Zero)
                UnhookWindowsHookEx(_hookId);
        };
    }

    #region Hotkey capture

    private void HotkeyBox_GotFocus(object sender, RoutedEventArgs e)
    {
        _isCapturingKey = true;
        HotkeyHint.Text = "キーを押して設定...";
        HotkeyBox.BorderBrush = BorderBrushActive;
    }

    private void HotkeyBox_LostFocus(object sender, RoutedEventArgs e)
    {
        _isCapturingKey = false;
        HotkeyHint.Text = "";
        HotkeyBox.BorderBrush = BorderBrushNormal;
    }

    private static bool IsModifierVk(int vk) =>
        vk is VK_CONTROL or VK_SHIFT or VK_MENU
            or 0xA0 or 0xA1   // LShift, RShift
            or 0xA2 or 0xA3   // LControl, RControl
            or 0xA4 or 0xA5;  // LAlt, RAlt

    private static ModifierKeys GetCurrentModifiers()
    {
        var modifiers = ModifierKeys.None;
        if ((GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0) modifiers |= ModifierKeys.Control;
        if ((GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0) modifiers |= ModifierKeys.Shift;
        if ((GetAsyncKeyState(VK_MENU) & 0x8000) != 0) modifiers |= ModifierKeys.Alt;
        return modifiers;
    }

    private void CaptureHotkey(int vk)
    {
        _registeredKey = KeyInterop.KeyFromVirtualKey(vk);
        _registeredModifiers = GetCurrentModifiers();
        _isCapturingKey = false;
        UpdateHotkeyDisplay();
        HotkeyHint.Text = "";
        HotkeyBox.BorderBrush = BorderBrushNormal;
        Keyboard.ClearFocus();
    }

    private void UpdateHotkeyDisplay()
    {
        var parts = new List<string>();
        if (_registeredModifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
        if (_registeredModifiers.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
        if (_registeredModifiers.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
        parts.Add(_registeredKey.ToString());
        HotkeyBox.Text = string.Join(" + ", parts);
    }

    #endregion

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (nint)WM_KEYDOWN)
        {
            var pressedVk = Marshal.ReadInt32(lParam);

            if (!IsModifierVk(pressedVk))
            {
                if (_isCapturingKey)
                {
                    Dispatcher.BeginInvoke(() => CaptureHotkey(pressedVk));
                }
                else
                {
                    var modifiers = GetCurrentModifiers();
                    var selectedVk = KeyInterop.VirtualKeyFromKey(_registeredKey);

                    if (pressedVk == selectedVk && modifiers == _registeredModifiers)
                        Dispatcher.BeginInvoke(OnLapKeyPressed);
                }
            }
        }
        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private void OnLapKeyPressed()
    {
        if (!_isRunning)
        {
            _totalOffset = TimeSpan.Zero;
            _totalStopwatch.Restart();
            _lapOffset = TimeSpan.Zero;
            _lapStopwatch.Restart();
            _lapCount = 0;
            _isRunning = true;
            _isStopped = false;
            _displayTimer.Start();
        }
        else if (_isStopped)
        {
            _totalStopwatch.Start();
            _lapOffset = TimeSpan.Zero;
            _lapStopwatch.Restart();
            _isStopped = false;
            _displayTimer.Start();
        }
        else
        {
            RecordLap();
            _lapOffset = TimeSpan.Zero;
            _lapStopwatch.Restart();
        }
    }

    private void RecordLap()
    {
        _lapCount++;
        var lapTime = LapElapsed;
        LapList.Items.Insert(0, $"Lap {_lapCount,3}   {FormatLapTime(lapTime)}");
        UpdateSpeed();
    }

    private void DisplayTimer_Tick(object? sender, EventArgs e)
    {
        var total = FormatTotalTime(TotalElapsed);
        if (TotalTimeText.Text != total)
            TotalTimeText.Text = total;

        var lap = FormatLapTime(LapElapsed);
        if (LapTimeText.Text != lap)
            LapTimeText.Text = lap;

        UpdateDiff();
    }

    private void UpdateDiff()
    {
        if (!_isRunning)
        {
            if (DiffText.Text.Length > 0)
                DiffText.Text = "";
            return;
        }

        var totalHours = TotalElapsed.TotalHours;
        var nextLapCount = _lapCount + 1;
        var expectedHours = (double)nextLapCount * PerLapValue / TargetSpeed;
        var marginSeconds = (expectedHours - totalHours) * 3600.0;

        string text;
        SolidColorBrush brush;
        if (marginSeconds >= 0)
        {
            text = $"+{marginSeconds:F1}s";
            brush = SpeedBrushAhead;
        }
        else
        {
            text = $"{marginSeconds:F1}s";
            brush = SpeedBrushBehind;
        }

        if (DiffText.Text != text)
        {
            DiffText.Text = text;
            DiffText.Foreground = brush;
        }
    }

    private void UpdateSpeed()
    {
        var totalHours = TotalElapsed.TotalHours;
        if (totalHours > 0 && _lapCount > 0)
        {
            var speed = (long)(_lapCount * PerLapValue / totalHours);
            SpeedText.Text = $"{speed:N0} /h";

            var expectedHours = (double)_lapCount * PerLapValue / TargetSpeed;
            var delayMinutes = (totalHours - expectedHours) * 60.0;

            SpeedText.Foreground = delayMinutes <= 0 ? SpeedBrushAhead
                                 : delayMinutes <= 1 ? SpeedBrushSlightlyBehind
                                 : SpeedBrushBehind;
        }
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning && !_isStopped)
        {
            _totalStopwatch.Stop();
            _lapStopwatch.Stop();
            _displayTimer.Stop();
            _isStopped = true;
        }
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        _totalOffset = TimeSpan.Zero;
        _totalStopwatch.Reset();
        _lapOffset = TimeSpan.Zero;
        _lapStopwatch.Reset();
        _displayTimer.Stop();
        _isRunning = false;
        _isStopped = false;
        _lapCount = 0;

        TotalTimeText.Text = "00:00:00.0";
        LapTimeText.Text = "00:00.0";
        SpeedText.Text = "0 /h";
        SpeedText.Foreground = SpeedBrushAhead;
        DiffText.Text = "";
        LapList.Items.Clear();
        DeleteSaveFile();
    }

    #region Save / Load

    private sealed class SaveData
    {
        public long TotalElapsedTicks { get; set; }
        public long LapElapsedTicks { get; set; }
        public int LapCount { get; set; }
        public List<string> LapHistory { get; set; } = [];
        public int HotkeyVk { get; set; }
        public int HotkeyModifiers { get; set; }
        public string SpeedDisplay { get; set; } = "";
    }

    private void SaveState()
    {
        try
        {
            if (!_isRunning)
            {
                DeleteSaveFile();
                return;
            }

            var data = new SaveData
            {
                TotalElapsedTicks = TotalElapsed.Ticks,
                LapElapsedTicks = LapElapsed.Ticks,
                LapCount = _lapCount,
                HotkeyVk = KeyInterop.VirtualKeyFromKey(_registeredKey),
                HotkeyModifiers = (int)_registeredModifiers,
                SpeedDisplay = SpeedText.Text,
            };

            foreach (var item in LapList.Items)
            {
                if (item is string s)
                    data.LapHistory.Add(s);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(SaveFilePath)!);
            File.WriteAllText(SaveFilePath, JsonSerializer.Serialize(data));
        }
        catch
        {
            // Ignore save errors
        }
    }

    private void LoadState()
    {
        try
        {
            if (!File.Exists(SaveFilePath)) return;

            var json = File.ReadAllText(SaveFilePath);
            var data = JsonSerializer.Deserialize<SaveData>(json);
            if (data is null || data.TotalElapsedTicks == 0) return;

            _totalOffset = TimeSpan.FromTicks(data.TotalElapsedTicks);
            _totalStopwatch.Reset();
            _lapOffset = TimeSpan.FromTicks(data.LapElapsedTicks);
            _lapStopwatch.Reset();

            _lapCount = data.LapCount;
            _isRunning = true;
            _isStopped = true;

            var key = KeyInterop.KeyFromVirtualKey(data.HotkeyVk);
            if (key != Key.None)
            {
                _registeredKey = key;
                _registeredModifiers = (ModifierKeys)data.HotkeyModifiers;
                UpdateHotkeyDisplay();
            }

            TotalTimeText.Text = FormatTotalTime(TotalElapsed);
            LapTimeText.Text = FormatLapTime(LapElapsed);

            if (!string.IsNullOrEmpty(data.SpeedDisplay))
                SpeedText.Text = data.SpeedDisplay;

            if (_lapCount > 0)
            {
                var totalHours = TotalElapsed.TotalHours;
                var expectedHours = (double)_lapCount * PerLapValue / TargetSpeed;
                var delayMinutes = (totalHours - expectedHours) * 60.0;
                SpeedText.Foreground = delayMinutes <= 0 ? SpeedBrushAhead
                                     : delayMinutes <= 1 ? SpeedBrushSlightlyBehind
                                     : SpeedBrushBehind;
            }

            UpdateDiff();

            foreach (var lap in data.LapHistory)
                LapList.Items.Add(lap);
        }
        catch
        {
            // Ignore load errors — start fresh
        }
    }

    private static void DeleteSaveFile()
    {
        try { File.Delete(SaveFilePath); } catch { }
    }

    #endregion

    private static string FormatTotalTime(TimeSpan ts) =>
        string.Create(10, ts, static (buf, t) =>
        {
            int h = (int)t.TotalHours;
            buf[0] = (char)('0' + h / 10);
            buf[1] = (char)('0' + h % 10);
            buf[2] = ':';
            buf[3] = (char)('0' + t.Minutes / 10);
            buf[4] = (char)('0' + t.Minutes % 10);
            buf[5] = ':';
            buf[6] = (char)('0' + t.Seconds / 10);
            buf[7] = (char)('0' + t.Seconds % 10);
            buf[8] = '.';
            buf[9] = (char)('0' + t.Milliseconds / 100);
        });

    private static string FormatLapTime(TimeSpan ts) =>
        string.Create(7, ts, static (buf, t) =>
        {
            int m = (int)t.TotalMinutes;
            buf[0] = (char)('0' + m / 10);
            buf[1] = (char)('0' + m % 10);
            buf[2] = ':';
            buf[3] = (char)('0' + t.Seconds / 10);
            buf[4] = (char)('0' + t.Seconds % 10);
            buf[5] = '.';
            buf[6] = (char)('0' + t.Milliseconds / 100);
        });
}