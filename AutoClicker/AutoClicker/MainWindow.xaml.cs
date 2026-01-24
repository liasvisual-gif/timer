using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AutoClicker.Helpers;
using AutoClicker.Models;

namespace AutoClicker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<ClickPoint> _clickPoints = new ObservableCollection<ClickPoint>();
        private ObservableCollection<OrderClickPoint> _orderPoints = new ObservableCollection<OrderClickPoint>();
        private ObservableCollection<object> _orderSequence = new ObservableCollection<object>();
        private ProfileManager _profileManager = new ProfileManager();
        private GlobalHotkey _globalHotkey = new GlobalHotkey();
        private int _nextHotkeyId = 1;
        private Key _selectedKey = Key.None;
        private Key _orderSelectedKey = Key.None;
        private Key _emergencyKey = Key.Escape;
        private string _currentProfileName = "デフォルト";
        private const int MOUSE_POSITION_HOTKEY_ID = 9999;
        private const int EMERGENCY_STOP_HOTKEY_ID = 9998;
        private bool _isEmergencyStopped = false;
        private ClickPoint? _editingClickPoint = null;
        private OrderClickPoint? _editingOrderPoint = null;
        private int _joyButtonNumber = 0;

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        public MainWindow()
        {
            InitializeComponent();
            ClickPointsDataGrid.ItemsSource = _clickPoints;
            OrderPointsDataGrid.ItemsSource = _orderPoints;
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        private void ClickPointsDataGrid_LoadingRow(object? sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // XAMLの要素が初期化された後にItemSourceを設定
            AvailableClickPointsDataGrid.ItemsSource = _clickPoints;
            OrderSequenceDataGrid.ItemsSource = _orderSequence;
            
            var windowHandle = new WindowInteropHelper(this).Handle;
            var source = HwndSource.FromHwnd(windowHandle);
            if (source != null)
            {
                _globalHotkey.Initialize(windowHandle, source);
                
                // 緊急停止キーを最優先で登録
                RegisterEmergencyKey();
                
                // Qキーでマウス位置取得のホットキー登録
                uint qKeyCode = (uint)KeyInterop.VirtualKeyFromKey(Key.Q);
                _globalHotkey.Register(MOUSE_POSITION_HOTKEY_ID, GlobalHotkey.MOD_NONE, qKeyCode, GetMousePositionHotkey);
            }

            LoadProfiles();
        }

        private void RegisterEmergencyKey()
        {
            uint escKeyCode = (uint)KeyInterop.VirtualKeyFromKey(_emergencyKey);
            _globalHotkey.Register(EMERGENCY_STOP_HOTKEY_ID, GlobalHotkey.MOD_NONE, escKeyCode, ToggleEmergencyStop);
            EmergencyKeyTextBox.Text = _emergencyKey.ToString();
        }

        private void ToggleEmergencyStop()
        {
            _isEmergencyStopped = !_isEmergencyStopped;
            
            Dispatcher.Invoke(() =>
            {
                if (_isEmergencyStopped)
                {
                    // すべての連打を強制停止
                    foreach (var point in _clickPoints)
                    {
                        if (point.IsRapidClickActive)
                        {
                            point.RapidClickCancellation?.Cancel();
                            point.IsRapidClickActive = false;
                        }
                    }
                    
                    EmergencyStatusTextBlock.Text = "■ 無効";
                    EmergencyStatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                    StatusTextBlock.Text = "⚠️ 無効化中 - すべてのクリック機能が無効化されています";
                    StatusTextBlock.Background = new SolidColorBrush(Colors.LightCoral);
                    ClickPointsDataGrid.Items.Refresh();
                }
                else
                {
                    EmergencyStatusTextBlock.Text = "■ 有効";
                    EmergencyStatusTextBlock.Foreground = new SolidColorBrush(Colors.Green);
                    StatusTextBlock.Text = "準備完了";
                    StatusTextBlock.Background = new SolidColorBrush(Colors.LightGray);
                }
            });
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _globalHotkey.Dispose();
        }

        private void LoadProfiles()
        {
            var profiles = _profileManager.GetProfileNames();
            ProfileComboBox.ItemsSource = profiles;
            
            if (profiles.Count > 0)
            {
                ProfileComboBox.SelectedIndex = 0;
            }
        }

        private void GetMousePositionHotkey()
        {
            Dispatcher.Invoke(() =>
            {
                if (GetCursorPos(out POINT point))
                {
                    XTextBox.Text = point.X.ToString();
                    YTextBox.Text = point.Y.ToString();
                    StatusTextBlock.Text = $"マウス位置取得: ({point.X}, {point.Y})";
                }
            });
        }

        private void DeviceTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (KeyTextBox == null) return; // InitializeComponent中は何もしない
            
            if (DeviceTypeComboBox.SelectedIndex == 0) // Keyboard
            {
                KeyTextBox.IsReadOnly = true;
                KeyTextBox.Text = _selectedKey != Key.None ? _selectedKey.ToString() : "";
            }
            else // Joystick
            {
                KeyTextBox.IsReadOnly = false;
                KeyTextBox.Text = _joyButtonNumber.ToString();
            }
        }

        private void KeyTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Joystickモードの場合は数値入力を許可
            if (DeviceTypeComboBox.SelectedIndex == 1)
            {
                return;
            }

            e.Handled = true;

            Key key = (e.Key == Key.System) ? e.SystemKey : e.Key;

            if (key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LWin || key == Key.RWin)
            {
                return;
            }

            _selectedKey = key;
            KeyTextBox.Text = key.ToString();
        }

        private void OrderKeyTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;

            Key key = (e.Key == Key.System) ? e.SystemKey : e.Key;

            if (key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LWin || key == Key.RWin)
            {
                return;
            }

            _orderSelectedKey = key;
            OrderKeyTextBox.Text = key.ToString();
        }

        private void EmergencyKeyTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;

            Key key = (e.Key == Key.System) ? e.SystemKey : e.Key;

            if (key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LWin || key == Key.RWin)
            {
                return;
            }

            // 既存の緊急停止キーを解除
            _globalHotkey.Unregister(EMERGENCY_STOP_HOTKEY_ID);

            // 新しいキーを設定
            _emergencyKey = key;
            uint keyCode = (uint)KeyInterop.VirtualKeyFromKey(_emergencyKey);
            bool registered = _globalHotkey.Register(EMERGENCY_STOP_HOTKEY_ID, GlobalHotkey.MOD_NONE, keyCode, ToggleEmergencyStop);

            if (registered)
            {
                EmergencyKeyTextBox.Text = _emergencyKey.ToString();
                StatusTextBlock.Text = $"緊急停止キーを {_emergencyKey} に変更しました";
            }
            else
            {
                MessageBox.Show($"キー {key} の登録に失敗しました。\n既に使用されている可能性があります。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                // 元のキーを再登録
                RegisterEmergencyKey();
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(XTextBox.Text, out int x) || !int.TryParse(YTextBox.Text, out int y))
            {
                MessageBox.Show("X座標とY座標は数値で入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string deviceType = ((ComboBoxItem)DeviceTypeComboBox.SelectedItem).Content.ToString() ?? "Keyboard";
            
            if (deviceType == "Keyboard")
            {
                if (_selectedKey == Key.None)
                {
                    MessageBox.Show("キーを選択してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            else // Joystick
            {
                if (!int.TryParse(KeyTextBox.Text, out int joyButton) || joyButton < 0)
                {
                    MessageBox.Show("ジョイスティックのボタン番号を0以上の数値で入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                _joyButtonNumber = joyButton;
            }

            if (!double.TryParse(RapidSpeedTextBox.Text, out double rapidSpeed) || rapidSpeed <= 0 || rapidSpeed > 60)
            {
                MessageBox.Show("連打速度は1～60の範囲で入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int rapidInterval = (int)Math.Round(1000.0 / rapidSpeed);

            int hotkeyId = _nextHotkeyId++;
            
            // 修飾キーの設定（Keyboardのみ）
            uint modifiers = GlobalHotkey.MOD_NONE;
            if (deviceType == "Keyboard")
            {
                if (CtrlCheckBox.IsChecked == true) modifiers |= GlobalHotkey.MOD_CONTROL;
                if (ShiftCheckBox.IsChecked == true) modifiers |= GlobalHotkey.MOD_SHIFT;
                if (AltCheckBox.IsChecked == true) modifiers |= GlobalHotkey.MOD_ALT;
            }

            var clickPoint = new ClickPoint
            {
                X = x,
                Y = y,
                DeviceType = deviceType,
                KeyName = deviceType == "Keyboard" ? _selectedKey.ToString() : $"Button {_joyButtonNumber}",
                JoyButtonNumber = deviceType == "Joystick" ? _joyButtonNumber : 0,
                Description = string.IsNullOrWhiteSpace(DescriptionTextBox.Text) ? $"クリック {hotkeyId}" : DescriptionTextBox.Text,
                HotkeyId = hotkeyId,
                ClickDelay = 0, // 常に0固定
                RapidClickInterval = rapidInterval,
                UseCtrl = deviceType == "Keyboard" && CtrlCheckBox.IsChecked == true,
                UseShift = deviceType == "Keyboard" && ShiftCheckBox.IsChecked == true,
                UseAlt = deviceType == "Keyboard" && AltCheckBox.IsChecked == true
            };

            bool registered = false;
            if (deviceType == "Keyboard")
            {
                uint vkCode = (uint)KeyInterop.VirtualKeyFromKey(_selectedKey);
                registered = _globalHotkey.Register(hotkeyId, modifiers, vkCode, async () =>
                {
                    if (!_isEmergencyStopped && !clickPoint.IsRapidClickActive)
                    {
                        clickPoint.IsRapidClickActive = true;
                        clickPoint.RapidClickCancellation = new CancellationTokenSource();
                        Dispatcher.Invoke(() =>
                        {
                            StatusTextBlock.Text = $"連打開始: {clickPoint.Description}";
                            ClickPointsDataGrid.Items.Refresh();
                        });
                        
                        await MouseHelper.StartRapidClickWhileKeyPressedAsync(clickPoint.X, clickPoint.Y, 
                            clickPoint.RapidClickInterval, 0, (int)vkCode, clickPoint.RapidClickCancellation.Token);
                        
                        clickPoint.IsRapidClickActive = false;
                        Dispatcher.Invoke(() =>
                        {
                            StatusTextBlock.Text = $"連打停止: {clickPoint.Description}";
                            ClickPointsDataGrid.Items.Refresh();
                        });
                    }
                });
            }
            else // Joystick
            {
                // Joystickの場合はポーリングで監視（簡易実装）
                registered = true; // Joystickは常に登録成功とする
                // TODO: 実際のJoystick入力監視を実装
            }

            if (registered)
            {
                _clickPoints.Add(clickPoint);
                ClearInputFields();
                StatusTextBlock.Text = $"追加: {clickPoint.Description} ({GetKeyDisplayName(clickPoint)})";
            }
            else
            {
                MessageBox.Show($"ホットキー {GetKeyDisplayName(clickPoint)} の登録に失敗しました。\n既に使用されている可能性があります。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetKeyDisplayName(ClickPoint clickPoint)
        {
            var parts = new List<string>();
            if (clickPoint.UseCtrl) parts.Add("Ctrl");
            if (clickPoint.UseShift) parts.Add("Shift");
            if (clickPoint.UseAlt) parts.Add("Alt");
            parts.Add(clickPoint.KeyName);
            return string.Join(" + ", parts);
        }

        private void AddToSequenceButton_Click(object sender, RoutedEventArgs e)
        {
            if (AvailableClickPointsDataGrid.SelectedItem is ClickPoint selectedPoint)
            {
                int order = _orderSequence.Count + 1;
                _orderSequence.Add(new
                {
                    Order = order,
                    Description = selectedPoint.Description,
                    ClickPointId = selectedPoint.HotkeyId
                });
                
                OrderSequenceDataGrid.Items.Refresh();
                StatusTextBlock.Text = $"追加: {selectedPoint.Description} (順番 {order})";
            }
            else
            {
                MessageBox.Show("左側の定点クリックから選択してください。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void RemoveFromSequenceButton_Click(object sender, RoutedEventArgs e)
        {
            if (OrderSequenceDataGrid.SelectedItem != null)
            {
                _orderSequence.Remove(OrderSequenceDataGrid.SelectedItem);
                
                // 順番を振り直す
                var tempList = _orderSequence.ToList();
                _orderSequence.Clear();
                int order = 1;
                foreach (var item in tempList)
                {
                    dynamic d = item;
                    _orderSequence.Add(new
                    {
                        Order = order++,
                        Description = d.Description,
                        ClickPointId = d.ClickPointId
                    });
                }
                
                OrderSequenceDataGrid.Items.Refresh();
                StatusTextBlock.Text = "除外しました";
            }
            else
            {
                MessageBox.Show("真ん中の順番リストから選択してください。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CreateOrderButton_Click(object sender, RoutedEventArgs e)
        {
            if (_orderSequence.Count < 2)
            {
                MessageBox.Show("オーダークリックには2つ以上のポイントが必要です。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_orderSelectedKey == Key.None)
            {
                MessageBox.Show("キーを選択してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            uint vkCode = (uint)KeyInterop.VirtualKeyFromKey(_orderSelectedKey);
            int hotkeyId = _nextHotkeyId++;

            var orderPoint = new OrderClickPoint
            {
                KeyName = _orderSelectedKey.ToString(),
                Description = string.IsNullOrWhiteSpace(OrderDescriptionTextBox.Text) ? $"オーダー {hotkeyId}" : OrderDescriptionTextBox.Text,
                HotkeyId = hotkeyId
            };

            foreach (var item in _orderSequence)
            {
                dynamic d = item;
                orderPoint.ClickPointIds.Add((int)d.ClickPointId);
            }

            bool registered = _globalHotkey.Register(hotkeyId, GlobalHotkey.MOD_NONE, vkCode, async () =>
            {
                if (!_isEmergencyStopped)
                {
                    var clickSequence = orderPoint.ClickPointIds
                        .Select(id => _clickPoints.FirstOrDefault(p => p.HotkeyId == id))
                        .Where(p => p != null)
                        .Select(p => (p!.X, p.Y, p.RapidClickInterval, p.ClickDelay));
                    
                    await MouseHelper.ClickSequentiallyAsync(clickSequence);
                }
            });

            if (registered)
            {
                _orderPoints.Add(orderPoint);
                _orderSequence.Clear();
                OrderKeyTextBox.Clear();
                OrderDescriptionTextBox.Clear();
                _orderSelectedKey = Key.None;
                StatusTextBlock.Text = $"オーダー作成: {orderPoint.Description} ({orderPoint.ClickPointIds.Count} ポイント)";
            }
            else
            {
                MessageBox.Show($"ホットキー {_orderSelectedKey} の登録に失敗しました。\n既に使用されている可能性があります。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ClickPointsDataGrid.SelectedItem is ClickPoint clickPoint)
            {
                _globalHotkey.Unregister(clickPoint.HotkeyId);
                _clickPoints.Remove(clickPoint);
                StatusTextBlock.Text = $"削除: {clickPoint.Description}";
            }
            else if (OrderPointsDataGrid.SelectedItem is OrderClickPoint orderPoint)
            {
                _globalHotkey.Unregister(orderPoint.HotkeyId);
                _orderPoints.Remove(orderPoint);
                StatusTextBlock.Text = $"削除: {orderPoint.Description}";
            }
            else
            {
                MessageBox.Show("削除する項目を選択してください。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void GetMousePositionButton_Click(object sender, RoutedEventArgs e)
        {
            if (GetCursorPos(out POINT point))
            {
                XTextBox.Text = point.X.ToString();
                YTextBox.Text = point.Y.ToString();
                StatusTextBlock.Text = $"マウス位置取得: ({point.X}, {point.Y})";
            }
        }

        private void ClickPointsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 説明欄のクリックでは編集モードにしない
            if (e.AddedItems.Count == 0) return;
            
            if (ClickPointsDataGrid.SelectedItem is ClickPoint clickPoint)
            {
                _editingClickPoint = clickPoint;
                _editingOrderPoint = null;
                
                XTextBox.Text = clickPoint.X.ToString();
                YTextBox.Text = clickPoint.Y.ToString();
                RapidSpeedTextBox.Text = clickPoint.RapidSpeed.ToString("F1");
                DescriptionTextBox.Text = clickPoint.Description;
                
                // デバイスタイプを設定
                DeviceTypeComboBox.SelectedIndex = clickPoint.DeviceType == "Keyboard" ? 0 : 1;
                
                if (clickPoint.DeviceType == "Keyboard")
                {
                    KeyTextBox.Text = clickPoint.KeyName;
                    CtrlCheckBox.IsChecked = clickPoint.UseCtrl;
                    ShiftCheckBox.IsChecked = clickPoint.UseShift;
                    AltCheckBox.IsChecked = clickPoint.UseAlt;
                    
                    try
                    {
                        _selectedKey = (Key)Enum.Parse(typeof(Key), clickPoint.KeyName);
                    }
                    catch
                    {
                        _selectedKey = Key.None;
                    }
                }
                else // Joystick
                {
                    KeyTextBox.Text = clickPoint.JoyButtonNumber.ToString();
                    _joyButtonNumber = clickPoint.JoyButtonNumber;
                    CtrlCheckBox.IsChecked = false;
                    ShiftCheckBox.IsChecked = false;
                    AltCheckBox.IsChecked = false;
                }
                
                StatusTextBlock.Text = $"編集モード: {clickPoint.Description}";
            }
        }

        private void OrderPointsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (OrderPointsDataGrid.SelectedItem is OrderClickPoint orderPoint)
            {
                // 選択されたオーダークリックの内容を真ん中に表示
                _orderSequence.Clear();
                int order = 1;
                
                foreach (var id in orderPoint.ClickPointIds)
                {
                    var point = _clickPoints.FirstOrDefault(p => p.HotkeyId == id);
                    if (point != null)
                    {
                        _orderSequence.Add(new
                        {
                            Order = order++,
                            Description = point.Description,
                            ClickPointId = point.HotkeyId
                        });
                    }
                }
                
                OrderSequenceDataGrid.Items.Refresh();
                StatusTextBlock.Text = $"オーダー選択: {orderPoint.Description}";
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (_editingClickPoint == null)
            {
                MessageBox.Show("編集する項目を選択してください。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!int.TryParse(XTextBox.Text, out int x) || !int.TryParse(YTextBox.Text, out int y))
            {
                MessageBox.Show("X座標とY座標は数値で入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!double.TryParse(RapidSpeedTextBox.Text, out double rapidSpeed) || rapidSpeed <= 0 || rapidSpeed > 60)
            {
                MessageBox.Show("連打速度は1～60の範囲で入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int rapidInterval = (int)Math.Round(1000.0 / rapidSpeed);

            // 連打中の場合は停止
            if (_editingClickPoint.IsRapidClickActive)
            {
                _editingClickPoint.RapidClickCancellation?.Cancel();
                _editingClickPoint.IsRapidClickActive = false;
            }

            // 修飾キーの設定
            uint modifiers = GlobalHotkey.MOD_NONE;
            if (CtrlCheckBox.IsChecked == true) modifiers |= GlobalHotkey.MOD_CONTROL;
            if (ShiftCheckBox.IsChecked == true) modifiers |= GlobalHotkey.MOD_SHIFT;
            if (AltCheckBox.IsChecked == true) modifiers |= GlobalHotkey.MOD_ALT;

            // キーまたは修飾キーが変更された場合、ホットキーを再登録
            bool keyChanged = _editingClickPoint.KeyName != _selectedKey.ToString() ||
                              _editingClickPoint.UseCtrl != (CtrlCheckBox.IsChecked == true) ||
                              _editingClickPoint.UseShift != (ShiftCheckBox.IsChecked == true) ||
                              _editingClickPoint.UseAlt != (AltCheckBox.IsChecked == true);
            
            if (keyChanged)
            {
                _globalHotkey.Unregister(_editingClickPoint.HotkeyId);
                
                uint vkCode = (uint)KeyInterop.VirtualKeyFromKey(_selectedKey);
                bool registered = _globalHotkey.Register(_editingClickPoint.HotkeyId, modifiers, vkCode, async () =>
                {
                    if (!_isEmergencyStopped && !_editingClickPoint.IsRapidClickActive)
                    {
                        _editingClickPoint.IsRapidClickActive = true;
                        _editingClickPoint.RapidClickCancellation = new CancellationTokenSource();
                        Dispatcher.Invoke(() =>
                        {
                            StatusTextBlock.Text = $"連打開始: {_editingClickPoint.Description}";
                            ClickPointsDataGrid.Items.Refresh();
                        });
                        
                        await MouseHelper.StartRapidClickWhileKeyPressedAsync(_editingClickPoint.X, _editingClickPoint.Y, 
                            _editingClickPoint.RapidClickInterval, _editingClickPoint.ClickDelay, (int)vkCode, _editingClickPoint.RapidClickCancellation.Token);
                        
                        _editingClickPoint.IsRapidClickActive = false;
                        Dispatcher.Invoke(() =>
                        {
                            StatusTextBlock.Text = $"連打停止: {_editingClickPoint.Description}";
                            ClickPointsDataGrid.Items.Refresh();
                        });
                    }
                });

                if (!registered)
                {
                    MessageBox.Show($"ホットキー {GetKeyDisplayName(_editingClickPoint)} の登録に失敗しました。\n既に使用されている可能性があります。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    // 元のキーを再登録
                    uint oldModifiers = GlobalHotkey.MOD_NONE;
                    if (_editingClickPoint.UseCtrl) oldModifiers |= GlobalHotkey.MOD_CONTROL;
                    if (_editingClickPoint.UseShift) oldModifiers |= GlobalHotkey.MOD_SHIFT;
                    if (_editingClickPoint.UseAlt) oldModifiers |= GlobalHotkey.MOD_ALT;
                    uint oldVkCode = (uint)KeyInterop.VirtualKeyFromKey((Key)Enum.Parse(typeof(Key), _editingClickPoint.KeyName));
                    _globalHotkey.Register(_editingClickPoint.HotkeyId, oldModifiers, oldVkCode, async () =>
                    {
                        if (!_isEmergencyStopped && !_editingClickPoint.IsRapidClickActive)
                        {
                            _editingClickPoint.IsRapidClickActive = true;
                            _editingClickPoint.RapidClickCancellation = new CancellationTokenSource();
                            Dispatcher.Invoke(() =>
                            {
                                StatusTextBlock.Text = $"連打開始: {_editingClickPoint.Description}";
                                ClickPointsDataGrid.Items.Refresh();
                            });
                            
                            await MouseHelper.StartRapidClickWhileKeyPressedAsync(_editingClickPoint.X, _editingClickPoint.Y, 
                                _editingClickPoint.RapidClickInterval, _editingClickPoint.ClickDelay, (int)oldVkCode, _editingClickPoint.RapidClickCancellation.Token);
                            
                            _editingClickPoint.IsRapidClickActive = false;
                            Dispatcher.Invoke(() =>
                            {
                                StatusTextBlock.Text = $"連打停止: {_editingClickPoint.Description}";
                                ClickPointsDataGrid.Items.Refresh();
                            });
                        }
                    });
                    return;
                }
            }

            // データを更新
            _editingClickPoint.X = x;
            _editingClickPoint.Y = y;
            _editingClickPoint.KeyName = _selectedKey.ToString();
            _editingClickPoint.ClickDelay = 0; // 常に0固定
            _editingClickPoint.RapidClickInterval = rapidInterval;
            _editingClickPoint.UseCtrl = CtrlCheckBox.IsChecked == true;
            _editingClickPoint.UseShift = ShiftCheckBox.IsChecked == true;
            _editingClickPoint.UseAlt = AltCheckBox.IsChecked == true;
            _editingClickPoint.Description = string.IsNullOrWhiteSpace(DescriptionTextBox.Text) ? 
                _editingClickPoint.Description : DescriptionTextBox.Text;

            ClickPointsDataGrid.Items.Refresh();
            StatusTextBlock.Text = $"更新: {_editingClickPoint.Description}";
            MessageBox.Show("クリックポイントを更新しました。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            
            ClearInputFields();
            _editingClickPoint = null;
        }

        private void SaveProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new InputDialog("プロファイル名を入力してください", _currentProfileName);
            if (dialog.ShowDialog() == true)
            {
                string profileName = dialog.ResponseText;
                if (string.IsNullOrWhiteSpace(profileName))
                {
                    MessageBox.Show("プロファイル名を入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var profile = new Profile
                {
                    Name = profileName,
                    ClickPoints = new ObservableCollection<ClickPoint>(_clickPoints),
                    OrderPoints = new ObservableCollection<OrderClickPoint>(_orderPoints)
                };

                try
                {
                    _profileManager.SaveProfile(profile);
                    _currentProfileName = profileName;
                    LoadProfiles();
                    ProfileComboBox.SelectedItem = profileName;
                    StatusTextBlock.Text = $"プロファイル保存: {profileName}";
                    MessageBox.Show($"プロファイル '{profileName}' を保存しました。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"保存に失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProfileComboBox.SelectedItem is string profileName)
            {
                var result = MessageBox.Show($"プロファイル '{profileName}' を削除しますか？", "確認", 
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    _profileManager.DeleteProfile(profileName);
                    LoadProfiles();
                    StatusTextBlock.Text = $"プロファイル削除: {profileName}";
                }
            }
        }

        private void ProfileComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProfileComboBox.SelectedItem is string profileName)
            {
                var profile = _profileManager.LoadProfile(profileName);
                if (profile != null)
                {
                    // 既存のホットキーを解除
                    foreach (var point in _clickPoints)
                    {
                        _globalHotkey.Unregister(point.HotkeyId);
                    }
                    foreach (var order in _orderPoints)
                    {
                        _globalHotkey.Unregister(order.HotkeyId);
                    }

                    _clickPoints.Clear();
                    _orderPoints.Clear();

                    // プロファイルから読み込み
                    foreach (var point in profile.ClickPoints)
                    {
                        uint modifiers = GlobalHotkey.MOD_NONE;
                        if (point.UseCtrl) modifiers |= GlobalHotkey.MOD_CONTROL;
                        if (point.UseShift) modifiers |= GlobalHotkey.MOD_SHIFT;
                        if (point.UseAlt) modifiers |= GlobalHotkey.MOD_ALT;
                        
                        uint vkCode = (uint)KeyInterop.VirtualKeyFromKey((Key)Enum.Parse(typeof(Key), point.KeyName));
                        _globalHotkey.Register(point.HotkeyId, modifiers, vkCode, async () =>
                        {
                            if (!_isEmergencyStopped && !point.IsRapidClickActive)
                            {
                                point.IsRapidClickActive = true;
                                point.RapidClickCancellation = new CancellationTokenSource();
                                Dispatcher.Invoke(() =>
                                {
                                    StatusTextBlock.Text = $"連打開始: {point.Description}";
                                    ClickPointsDataGrid.Items.Refresh();
                                });
                                
                                await MouseHelper.StartRapidClickWhileKeyPressedAsync(point.X, point.Y, 
                                    point.RapidClickInterval, point.ClickDelay, (int)vkCode, point.RapidClickCancellation.Token);
                                
                                point.IsRapidClickActive = false;
                                Dispatcher.Invoke(() =>
                                {
                                    StatusTextBlock.Text = $"連打停止: {point.Description}";
                                    ClickPointsDataGrid.Items.Refresh();
                                });
                            }
                        });
                        _clickPoints.Add(point);
                    }

                    foreach (var order in profile.OrderPoints)
                    {
                        uint vkCode = (uint)KeyInterop.VirtualKeyFromKey((Key)Enum.Parse(typeof(Key), order.KeyName));
                        _globalHotkey.Register(order.HotkeyId, GlobalHotkey.MOD_NONE, vkCode, async () =>
                        {
                            if (!_isEmergencyStopped)
                            {
                                var clickSequence = order.ClickPointIds
                                    .Select(id => _clickPoints.FirstOrDefault(p => p.HotkeyId == id))
                                    .Where(p => p != null)
                                    .Select(p => (p!.X, p.Y, p.RapidClickInterval, p.ClickDelay));
                                
                                await MouseHelper.ClickSequentiallyAsync(clickSequence);
                            }
                        });
                        _orderPoints.Add(order);
                    }

                    _currentProfileName = profileName;
                    StatusTextBlock.Text = $"プロファイル読み込み: {profileName}";

                    if (_clickPoints.Count > 0 || _orderPoints.Count > 0)
                    {
                        _nextHotkeyId = Math.Max(
                            _clickPoints.Count > 0 ? _clickPoints.Max(p => p.HotkeyId) + 1 : 0,
                            _orderPoints.Count > 0 ? _orderPoints.Max(p => p.HotkeyId) + 1 : 0
                        );
                    }
                }
            }
        }

        private void ClearInputFields()
        {
            XTextBox.Clear();
            YTextBox.Clear();
            KeyTextBox.Clear();
            DescriptionTextBox.Clear();
            RapidSpeedTextBox.Text = "10";
            CtrlCheckBox.IsChecked = false;
            ShiftCheckBox.IsChecked = false;
            AltCheckBox.IsChecked = false;
            DeviceTypeComboBox.SelectedIndex = 0;
            _selectedKey = Key.None;
            _joyButtonNumber = 0;
        }
    }

    // 入力ダイアログクラス
    public class InputDialog : Window
    {
        private TextBox _textBox;
        public string ResponseText => _textBox.Text;

        public InputDialog(string question, string defaultAnswer = "")
        {
            Width = 400;
            Height = 150;
            Title = "入力";
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var grid = new Grid { Margin = new Thickness(10) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var questionLabel = new TextBlock { Text = question, Margin = new Thickness(0, 0, 0, 10) };
            Grid.SetRow(questionLabel, 0);
            grid.Children.Add(questionLabel);

            _textBox = new TextBox { Text = defaultAnswer, Margin = new Thickness(0, 0, 0, 10) };
            Grid.SetRow(_textBox, 1);
            grid.Children.Add(_textBox);

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var okButton = new Button { Content = "OK", Width = 75, Margin = new Thickness(5, 0, 0, 0) };
            okButton.Click += (s, e) => { DialogResult = true; Close(); };
            var cancelButton = new Button { Content = "キャンセル", Width = 75, Margin = new Thickness(5, 0, 0, 0) };
            cancelButton.Click += (s, e) => { DialogResult = false; Close(); };
            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            Grid.SetRow(buttonPanel, 2);
            grid.Children.Add(buttonPanel);

            Content = grid;
        }
    }
}

