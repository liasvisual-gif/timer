using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.IO;
using System.Collections.ObjectModel;
using System.Linq;

namespace audition_nagurisaki
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer detectionTimer;
        private List<OverlayWindow> overlayWindows = new List<OverlayWindow>();
        private KeyboardHook? keyboardHook;
        private InputManager? inputManager;

        private const string SettingsFileName = "coordinates_settings.json";
        private static readonly string SettingsFilePath = System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(Environment.ProcessPath ?? AppContext.BaseDirectory) ?? AppContext.BaseDirectory,
            SettingsFileName);
        
        private string currentAuditionTab = "Default"; // 現在選択されている情報タブ
        private int currentWeek = 1; // 現在の週（1-16）
        private bool isYamaAri = true; // 山あり/山なしモード
        
        // 現在アクティブな窓（1-3）
        private int activeWindow = 1;
        
        // アクティブな座標登録対象（設定タブ用）
        private string? activeCoordTarget = null; // "11", "12", "21", "22", "31", "32"
        
        // プリセットコレクション
        private ObservableCollection<ClickPreset> clickPresets = new();

        // パターンコレクション
        private ObservableCollection<ClickPattern> clickPatterns = new();

        // オーディション進行ComboBox（現在表示中の窓用）
        private Dictionary<int, ComboBox> yamaAriComboBoxes = new();
        private Dictionary<int, ComboBox> yamaNashiComboBoxes = new();

        // ボマー設定CheckBox（現在表示中の窓用）
        private Dictionary<int, CheckBox> bomberCheckBoxes = new();
        private HashSet<int> bomberWeeks = new() { 8, 13 };

        // 窓別オーディション進行設定
        private Dictionary<int, WindowProgressionSettings> windowProgressions = new();
        private int currentProgressionWindow = 1;


        // 各窓の登録色（2座標から取得）
        private Color window1_color1;
        private Color window1_color2;
        private Color window2_color1;
        private Color window2_color2;
        private Color window3_color1;
        private Color window3_color2;
        
        private bool color1_registered = false;
        private bool color2_registered = false;
        private bool color3_registered = false;

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, int dwExtraInfo);

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        public MainWindow()
        {
            InitializeComponent();
            dgPresets.ItemsSource = clickPresets;
            this.Title = "審査員殴り";
            SetJapaneseText();  // UIテキストを日本語に設定
            InitializeProgressionComboBoxes();
            LoadSettings();  // 起動時に設定を読み込み
            InitializeTimers();
            InitializeKeyboardHook();
        }

        private void TabControlAuditions_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (tabControlAuditions.SelectedItem == tabEreeBest)
                currentAuditionTab = "EreeBest";
            else if (tabControlAuditions.SelectedItem == tabSpotlight)
                currentAuditionTab = "Spotlight";
            else if (tabControlAuditions.SelectedItem == tabOdotte)
                currentAuditionTab = "Odotte";
            else if (tabControlAuditions.SelectedItem == tabLegend)
                currentAuditionTab = "Legend";
            else if (tabControlAuditions.SelectedItem == tabNanasai)
                currentAuditionTab = "Nanasai";
            else if (tabControlAuditions.SelectedItem == tabUtahime)
                currentAuditionTab = "Utahime";

            // InitializeComponent()の実行中はtxtStatusがまだnullの可能性があるためチェック
            if (txtStatus != null)
            {
                txtStatus.Text = $"オーディションタブを切り替えました: {currentAuditionTab}";
            }
        }

        private void SetJapaneseText()
        {
            // タイトルとヘッダー
            txtTitle.Text = "審査員殴り";
            tabSettings.Header = "設定";
            tabInfo.Header = "情報";
            grpWindow1.Header = "8窓1 識別座標";
            grpWindow2.Header = "8窓2 識別座標";
            grpWindow3.Header = "8窓3 識別座標";
            
            // 週情報
            grpWeekInfo.Header = "週情報";
            lblWeek.Text = "現在の週:";
            txtWeek.Text = ConvertWeekToText(currentWeek);
            
            
            // 情報タブ
            tabEreeBest.Header = "えれぇベスト";
            tabSpotlight.Header = "spotlight";
            tabOdotte.Header = "踊っていいとも";
            tabLegend.Header = "legend";
            tabNanasai.Header = "七彩";
            tabUtahime.Header = "歌姫";


            lblEreeBestTitle.Text = "オーディション殴り先ルール - えれぇベスト";
            lblSpotlightTitle.Text = "オーディション殴り先ルール - spotlight";
            lblOdotteTitle.Text = "オーディション殴り先ルール - 踊っていいとも";
            lblLegendTitle.Text = "オーディション殴り先ルール - legend";
            lblNanasaiTitle.Text = "オーディション殴り先ルール - 七彩";
            lblUtahimeTitle.Text = "オーディション殴り先ルール - 歌姫";
            
            
            // ボタン
            btnSaveSettings.Content = "設定保存";
            btnLoadSettings.Content = "設定読込";
            btnLaunch.Content = "表示";
            btnClose.Content = "閉じる";
            
            // ステータス
            txtStatus.Text = "待機中...";
        }

        private void InitializeTimers()
        {
            // 色検出タイマー
            detectionTimer = new DispatcherTimer();
            detectionTimer.Interval = TimeSpan.FromMilliseconds(100);
            detectionTimer.Tick += DetectionTimer_Tick;
        }

        private void InitializeKeyboardHook()
        {
            inputManager = new InputManager();
            inputManager.InputPressed += InputManager_InputPressed;
            inputManager.Start();
        }

        private void InputManager_InputPressed(object? sender, string inputString)
        {
            Dispatcher.Invoke(() =>
            {
                // デバッグ用ログ
                txtStatus.Text = $"入力検出: {inputString}";

                // 窓移動キーチェック
                if (InputMatches(inputString, txtMoveToWindow1Key.Text))
                {
                    SetActiveWindow(1);
                    return;
                }
                if (InputMatches(inputString, txtMoveToWindow2Key.Text))
                {
                    SetActiveWindow(2);
                    return;
                }
                if (InputMatches(inputString, txtMoveToWindow3Key.Text))
                {
                    SetActiveWindow(3);
                    return;
                }
                if (!string.IsNullOrWhiteSpace(txtMoveToWindowSKey.Text) && InputMatches(inputString, txtMoveToWindowSKey.Text))
                {
                    SetActiveWindow(0);
                    return;
                }
                if (!string.IsNullOrWhiteSpace(txtMoveToWindow3W1Key.Text) && InputMatches(inputString, txtMoveToWindow3W1Key.Text))
                {
                    SetActiveWindow(4);
                    return;
                }
                if (!string.IsNullOrWhiteSpace(txtMoveToWindow3W2Key.Text) && InputMatches(inputString, txtMoveToWindow3W2Key.Text))
                {
                    SetActiveWindow(5);
                    return;
                }
                if (!string.IsNullOrWhiteSpace(txtMoveToWindow3W3Key.Text) && InputMatches(inputString, txtMoveToWindow3W3Key.Text))
                {
                    SetActiveWindow(6);
                    return;
                }

                // 判別キーチェック（アクティブ窓のみ）
                if (InputMatches(inputString, txtJudgeKey.Text))
                {
                    PerformWindowJudgement(activeWindow);
                    return;
                }

                // 週の進む/戻るホットキー
                if (inputString == "Left")
                {
                    BtnWeekBack_Click(null, null);
                    return;
                }
                if (inputString == "Right")
                {
                    BtnWeekForward_Click(null, null);
                    return;
                }

                // 座標登録キー（統合）
                if (InputMatches(inputString, txtCoordRegKey.Text))
                {
                    if (activeCoordTarget != null)
                        RegisterCoordinate();
                    else if (dgPresets.SelectedIndex >= 0)
                        RegisterFixedClickCoordinate();
                    else
                        txtStatus.Text = "座標登録: アクティブな項目がありません";
                    return;
                }

                // 山あり/山なし切り替えキー
                if (!string.IsNullOrWhiteSpace(txtYamaToggleKey.Text) && InputMatches(inputString, txtYamaToggleKey.Text))
                {
                    BtnToggleYama_Click(null, null);
                    return;
                }

                // パターンホットキーチェック（窓一致のみ実行）
                foreach (var pattern in clickPatterns)
                {
                    if (!string.IsNullOrWhiteSpace(pattern.Hotkey) && InputMatches(inputString, pattern.Hotkey))
                    {
                        if (pattern.WindowNumber == -1 || pattern.WindowNumber == activeWindow)
                        {
                            _ = ExecuteFixedClicks(pattern);
                            if (pattern.AdvanceWeek)
                            {
                                // 週更新の直前に判別結果をオーバーレイに反映
                                PerformWindowJudgement(activeWindow);
                            }
                            else
                            {
                                txtStatus.Text = $"パターン実行: {pattern.Name}";
                            }
                            return;
                        }
                    }
                }
            });
        }
        
        private void SetActiveWindow(int windowNumber)
        {
            activeWindow = windowNumber;
            string windowName = windowNumber switch
            {
                0 => "単窓",
                4 => "3窓1",
                5 => "3窓2",
                6 => "3窓3",
                _ => $"8窓{windowNumber}"
            };
            txtCurrentWindow.Text = windowName;

            int comboIndex = windowNumber switch
            {
                1 => 0,
                2 => 1,
                3 => 2,
                0 => 3,
                4 => 4,
                5 => 5,
                6 => 6,
                _ => 0
            };
            if (cmbActiveWindow.SelectedIndex != comboIndex)
                cmbActiveWindow.SelectedIndex = comboIndex;

            txtStatus.Text = $"{windowName}に移動しました";

            // オデ進行タブの対象窓をアクティブ窓に同期
            if (yamaAriComboBoxes.Count > 0)
                SaveCurrentProgressionToDict();
            currentProgressionWindow = windowNumber;
            if (cmbProgressionWindow.SelectedIndex != comboIndex)
                cmbProgressionWindow.SelectedIndex = comboIndex;
            else
                RebuildProgressionUI();

            // 窓切替時にオーバーレイのボマー設定を更新
            var newBomber = new HashSet<int>(GetWindowProgression(windowNumber).BomberWeeks);
            foreach (var overlayWindow in overlayWindows)
            {
                overlayWindow.SetBomberWeeks(newBomber);
                overlayWindow.SetWeekInfo(currentWeek);
            }
        }

        private void BtnSwitchWindow_Click(object sender, RoutedEventArgs e)
        {
            int windowNumber = cmbActiveWindow.SelectedIndex switch
            {
                0 => 1,
                1 => 2,
                2 => 3,
                3 => 0,
                4 => 4,
                5 => 5,
                6 => 6,
                _ => 1
            };
            SetActiveWindow(windowNumber);
        }
        
        private void RegisterCoordinate()
        {
            if (activeCoordTarget == null)
            {
                txtStatus.Text = "座標登録: アクティブな項目がありません";
                return;
            }
            
            if (!GetCursorPos(out POINT point))
                return;
            
            switch (activeCoordTarget)
            {
                case "11":
                    txtX11.Text = point.X.ToString();
                    txtY11.Text = point.Y.ToString();
                    break;
                case "12":
                    txtX12.Text = point.X.ToString();
                    txtY12.Text = point.Y.ToString();
                    break;
                case "21":
                    txtX21.Text = point.X.ToString();
                    txtY21.Text = point.Y.ToString();
                    break;
                case "22":
                    txtX22.Text = point.X.ToString();
                    txtY22.Text = point.Y.ToString();
                    break;
                case "31":
                    txtX31.Text = point.X.ToString();
                    txtY31.Text = point.Y.ToString();
                    break;
                case "32":
                    txtX32.Text = point.X.ToString();
                    txtY32.Text = point.Y.ToString();
                    break;
                case "S1":
                    txtXS1.Text = point.X.ToString();
                    txtYS1.Text = point.Y.ToString();
                    break;
                case "S2":
                    txtXS2.Text = point.X.ToString();
                    txtYS2.Text = point.Y.ToString();
                    break;
                case "3W11":
                    txtX3W11.Text = point.X.ToString();
                    txtY3W11.Text = point.Y.ToString();
                    break;
                case "3W12":
                    txtX3W12.Text = point.X.ToString();
                    txtY3W12.Text = point.Y.ToString();
                    break;
                case "3W21":
                    txtX3W21.Text = point.X.ToString();
                    txtY3W21.Text = point.Y.ToString();
                    break;
                case "3W22":
                    txtX3W22.Text = point.X.ToString();
                    txtY3W22.Text = point.Y.ToString();
                    break;
                case "3W31":
                    txtX3W31.Text = point.X.ToString();
                    txtY3W31.Text = point.Y.ToString();
                    break;
                case "3W32":
                    txtX3W32.Text = point.X.ToString();
                    txtY3W32.Text = point.Y.ToString();
                    break;
            }
            
            txtStatus.Text = $"座標登録: {activeCoordTarget} X={point.X}, Y={point.Y}";
        }
        
        private void CoordRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb)
            {
                activeCoordTarget = rb.Name switch
                {
                    "rbCoord11" => "11",
                    "rbCoord12" => "12",
                    "rbCoord21" => "21",
                    "rbCoord22" => "22",
                    "rbCoord31" => "31",
                    "rbCoord32" => "32",
                    "rbCoordS1" => "S1",
                    "rbCoordS2" => "S2",
                    "rbCoord3W11" => "3W11",
                    "rbCoord3W12" => "3W12",
                    "rbCoord3W21" => "3W21",
                    "rbCoord3W22" => "3W22",
                    "rbCoord3W31" => "3W31",
                    "rbCoord3W32" => "3W32",
                    _ => null
                };
                
                if (txtCoordRegStatus != null && activeCoordTarget != null)
                {
                    if (activeCoordTarget.StartsWith("S"))
                    {
                        int c = int.Parse(activeCoordTarget[1].ToString());
                        txtCoordRegStatus.Text = $"(アクティブ: 単窓-座標{c})";
                    }
                    else if (activeCoordTarget.StartsWith("3W"))
                    {
                        int w = int.Parse(activeCoordTarget[2].ToString());
                        int c = int.Parse(activeCoordTarget[3].ToString());
                        txtCoordRegStatus.Text = $"(アクティブ: 3窓{w}-座標{c})";
                    }
                    else
                    {
                        int w = int.Parse(activeCoordTarget[0].ToString());
                        int c = int.Parse(activeCoordTarget[1].ToString());
                        txtCoordRegStatus.Text = $"(アクティブ: 8窓{w}-座標{c})";
                    }
                }
            }
        }
        
        private void DgPresets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgPresets.SelectedItem is ClickPreset preset)
            {
                activeCoordTarget = null;
                txtPresetName.Text = preset.Name;
                txtPresetX.Text = preset.X.ToString();
                txtPresetY.Text = preset.Y.ToString();
                txtPresetDelay.Text = preset.Delay.ToString();
                txtCoordRegStatus.Text = $"(アクティブ: {preset.Name})";
            }
            else
            {
                txtPresetName.Text = "";
                txtPresetX.Text = "";
                txtPresetY.Text = "";
                txtPresetDelay.Text = "";
                if (activeCoordTarget == null)
                    txtCoordRegStatus.Text = "(アクティブ: なし)";
            }
        }

        private void BtnAddPreset_Click(object sender, RoutedEventArgs e)
        {
            var preset = new ClickPreset { Name = $"定点{clickPresets.Count + 1}" };
            clickPresets.Add(preset);
            dgPresets.SelectedItem = preset;
        }

        private void BtnDeletePreset_Click(object sender, RoutedEventArgs e)
        {
            if (dgPresets.SelectedIndex >= 0)
            {
                string deletedName = clickPresets[dgPresets.SelectedIndex].Name;
                int idx = dgPresets.SelectedIndex;
                clickPresets.RemoveAt(idx);

                foreach (var pattern in clickPatterns)
                    pattern.PresetNames.RemoveAll(n => n == deletedName);

                if (clickPresets.Count > 0)
                    dgPresets.SelectedIndex = Math.Min(idx, clickPresets.Count - 1);
            }
        }

        private void BtnApplyPreset_Click(object sender, RoutedEventArgs e)
        {
            if (dgPresets.SelectedItem is ClickPreset preset)
            {
                string oldName = preset.Name;
                string newName = txtPresetName.Text;
                preset.Name = newName;
                preset.X = int.TryParse(txtPresetX.Text, out int x) ? x : 0;
                preset.Y = int.TryParse(txtPresetY.Text, out int y) ? y : 0;
                preset.Delay = int.TryParse(txtPresetDelay.Text, out int d) ? d : 0;

                if (oldName != newName)
                {
                    foreach (var pattern in clickPatterns)
                        for (int i = 0; i < pattern.PresetNames.Count; i++)
                            if (pattern.PresetNames[i] == oldName)
                                pattern.PresetNames[i] = newName;
                }

                dgPresets.Items.Refresh();
                txtStatus.Text = $"{preset.Name} 保存しました";
            }
        }

        private void BtnMovePresetUp_Click(object sender, RoutedEventArgs e)
        {
            int idx = dgPresets.SelectedIndex;
            if (idx > 0)
            {
                clickPresets.Move(idx, idx - 1);
                dgPresets.SelectedIndex = idx - 1;
            }
        }

        private void BtnMovePresetDown_Click(object sender, RoutedEventArgs e)
        {
            int idx = dgPresets.SelectedIndex;
            if (idx >= 0 && idx < clickPresets.Count - 1)
            {
                clickPresets.Move(idx, idx + 1);
                dgPresets.SelectedIndex = idx + 1;
            }
        }

        private void AdjustPresetField(TextBox textBox, int delta)
        {
            if (int.TryParse(textBox.Text, out int val))
                textBox.Text = (val + delta).ToString();
            else
                textBox.Text = "0";
        }

        private void BtnPresetDelayUp_Click(object sender, RoutedEventArgs e) => AdjustPresetField(txtPresetDelay, 1);
        private void BtnPresetDelayDown_Click(object sender, RoutedEventArgs e) => AdjustPresetField(txtPresetDelay, -1);
        
        private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // クリックされた要素がRadioButtonでない場合、アクティブ状態を解除
            if (e.OriginalSource is not RadioButton)
            {
                // 設定タブの座標登録アクティブを解除
                if (activeCoordTarget != null)
                {
                    activeCoordTarget = null;
                    rbCoord11.IsChecked = false;
                    rbCoord12.IsChecked = false;
                    rbCoord21.IsChecked = false;
                    rbCoord22.IsChecked = false;
                    rbCoord31.IsChecked = false;
                    rbCoord32.IsChecked = false;
                    rbCoordS1.IsChecked = false;
                    rbCoordS2.IsChecked = false;
                    rbCoord3W11.IsChecked = false;
                    rbCoord3W12.IsChecked = false;
                    rbCoord3W21.IsChecked = false;
                    rbCoord3W22.IsChecked = false;
                    rbCoord3W31.IsChecked = false;
                    rbCoord3W32.IsChecked = false;
                    txtCoordRegStatus.Text = "(アクティブ: なし)";
                }
                
                // 定点クリック座標登録アクティブを解除（プリセットエリア外クリック時）
                if (dgPresets.SelectedIndex >= 0 && !IsVisualDescendantOf(e.OriginalSource as DependencyObject, presetRegArea))
                {
                    dgPresets.SelectedIndex = -1;
                    if (activeCoordTarget == null)
                        txtCoordRegStatus.Text = "(アクティブ: なし)";
                }
            }
        }
        
        private static bool IsVisualDescendantOf(DependencyObject? child, DependencyObject parent)
        {
            while (child != null)
            {
                if (child == parent) return true;
                child = VisualTreeHelper.GetParent(child);
            }
            return false;
        }

        private bool InputMatches(string inputString, string targetKey)
        {
            string target = targetKey.ToUpper().Trim();
            string input = inputString.ToUpper();
            
            // 直接一致
            if (input == target)
                return true;

            // 数字キー対応
            if (input.StartsWith("D") && input.Length == 2 && char.IsDigit(input[1]))
            {
                string numberPart = input.Substring(1);
                if (target == numberPart)
                    return true;
            }
            
            // テンキー対応
            if (target.StartsWith("NUMPAD") || target.StartsWith("NUM"))
            {
                string numPart = target.Replace("NUMPAD", "").Replace("NUM", "");
                return input.Contains(numPart);
            }
            
            // スペースキー対応
            if (target == "SPACE" && input == "SPACE")
                return true;
            
            return false;
        }

        private void PerformJudgement()
        {
            if (overlayWindows.Count == 0)
            {
                txtStatus.Text = "エラー: 表示ウィンドウが起動していません";
                return;
            }

            try
            {
                // フォントサイズを適用
                if (int.TryParse(txtFontSize.Text, out int fontSize) && fontSize > 0)
                {
                    foreach (var overlayWindow in overlayWindows)
                    {
                        overlayWindow.SetFontSize(fontSize);
                    }
                }

                // 各窓の2座標を取得
                if (!int.TryParse(txtX11.Text, out int x11) || !int.TryParse(txtY11.Text, out int y11) ||
                    !int.TryParse(txtX12.Text, out int x12) || !int.TryParse(txtY12.Text, out int y12) ||
                    !int.TryParse(txtX21.Text, out int x21) || !int.TryParse(txtY21.Text, out int y21) ||
                    !int.TryParse(txtX22.Text, out int x22) || !int.TryParse(txtY22.Text, out int y22) ||
                    !int.TryParse(txtX31.Text, out int x31) || !int.TryParse(txtY31.Text, out int y31) ||
                    !int.TryParse(txtX32.Text, out int x32) || !int.TryParse(txtY32.Text, out int y32))
                {
                    txtStatus.Text = "エラー: 座標の形式が正しくありません";
                    return;
                }

                // 各座標の色を取得
                Color color11 = ColorDetector.GetColorAt(x11, y11);
                Color color12 = ColorDetector.GetColorAt(x12, y12);
                Color color21 = ColorDetector.GetColorAt(x21, y21);
                Color color22 = ColorDetector.GetColorAt(x22, y22);
                Color color31 = ColorDetector.GetColorAt(x31, y31);
                Color color32 = ColorDetector.GetColorAt(x32, y32);

                // 各座標のRGB最小値を判定
                string lowest11 = GetLowestRGB(color11);
                string lowest12 = GetLowestRGB(color12);
                string lowest21 = GetLowestRGB(color21);
                string lowest22 = GetLowestRGB(color22);
                string lowest31 = GetLowestRGB(color31);
                string lowest32 = GetLowestRGB(color32);

                // 情報タブのルールと照合
                string result1 = CheckRules(lowest11, lowest12, color11, color12, 1);
                string result2 = CheckRules(lowest21, lowest22, color21, color22, 2);
                string result3 = CheckRules(lowest31, lowest32, color31, color32, 3);

                // 結果を表示ウィンドウに更新
                foreach (var overlayWindow in overlayWindows)
                {
                    overlayWindow.UpdateJudgementResult(1, 1, $"{lowest11} ({result1})", color11);
                    overlayWindow.UpdateJudgementResult(1, 2, $"{lowest12} ({result1})", color12);
                    overlayWindow.UpdateJudgementResult(2, 1, $"{lowest21} ({result2})", color21);
                    overlayWindow.UpdateJudgementResult(2, 2, $"{lowest22} ({result2})", color22);
                    overlayWindow.UpdateJudgementResult(3, 1, $"{lowest31} ({result3})", color31);
                    overlayWindow.UpdateJudgementResult(3, 2, $"{lowest32} ({result3})", color32);
                }

                // メイン表示を更新（最初にマッチしたルールを表示）
                string resultToShow = !string.IsNullOrEmpty(result1) ? result1 :
                                      !string.IsNullOrEmpty(result2) ? result2 :
                                      !string.IsNullOrEmpty(result3) ? result3 : "";
                
                if (!string.IsNullOrEmpty(resultToShow))
                {
                    foreach (var overlayWindow in overlayWindows)
                    {
                        overlayWindow.ShowResultText(resultToShow);
                    }
                }

                // 週を自動的に更新（1-16でループ）
                currentWeek++;
                if (currentWeek > 16)
                {
                    currentWeek = 1;
                }
                txtWeek.Text = ConvertWeekToText(currentWeek);
                foreach (var overlayWindow in overlayWindows)
                {
                    overlayWindow.SetWeekInfo(currentWeek);
                }

                txtStatus.Text = $"判別完了 - {ConvertWeekToText(currentWeek)}";
            }
            catch (Exception ex)
            {
                txtStatus.Text = $"エラー: {ex.Message}";
            }
        }

        private void PerformWindowJudgement(int windowNumber)
        {
            if (overlayWindows.Count == 0)
            {
                txtStatus.Text = "表示ウィンドウが開いていません";
                return;
            }

            try
            {
                int x1 = 0, y1 = 0, x2 = 0, y2 = 0;

                switch (windowNumber)
                {
                    case 1:
                        if (!int.TryParse(txtX11.Text, out x1) || !int.TryParse(txtY11.Text, out y1) ||
                            !int.TryParse(txtX12.Text, out x2) || !int.TryParse(txtY12.Text, out y2))
                        {
                            txtStatus.Text = "8窓1の座標が設定されていません";
                            return;
                        }
                        break;
                    case 2:
                        if (!int.TryParse(txtX21.Text, out x1) || !int.TryParse(txtY21.Text, out y1) ||
                            !int.TryParse(txtX22.Text, out x2) || !int.TryParse(txtY22.Text, out y2))
                        {
                            txtStatus.Text = "8窓2の座標が設定されていません";
                            return;
                        }
                        break;
                    case 3:
                        if (!int.TryParse(txtX31.Text, out x1) || !int.TryParse(txtY31.Text, out y1) ||
                            !int.TryParse(txtX32.Text, out x2) || !int.TryParse(txtY32.Text, out y2))
                        {
                            txtStatus.Text = "8窓3の座標が設定されていません";
                            return;
                        }
                        break;
                    case 0:
                        if (!int.TryParse(txtXS1.Text, out x1) || !int.TryParse(txtYS1.Text, out y1) ||
                            !int.TryParse(txtXS2.Text, out x2) || !int.TryParse(txtYS2.Text, out y2))
                        {
                            txtStatus.Text = "単窓の座標が設定されていません";
                            return;
                        }
                        break;
                    case 4:
                        if (!int.TryParse(txtX3W11.Text, out x1) || !int.TryParse(txtY3W11.Text, out y1) ||
                            !int.TryParse(txtX3W12.Text, out x2) || !int.TryParse(txtY3W12.Text, out y2))
                        {
                            txtStatus.Text = "3窓1の座標が設定されていません";
                            return;
                        }
                        break;
                    case 5:
                        if (!int.TryParse(txtX3W21.Text, out x1) || !int.TryParse(txtY3W21.Text, out y1) ||
                            !int.TryParse(txtX3W22.Text, out x2) || !int.TryParse(txtY3W22.Text, out y2))
                        {
                            txtStatus.Text = "3窓2の座標が設定されていません";
                            return;
                        }
                        break;
                    case 6:
                        if (!int.TryParse(txtX3W31.Text, out x1) || !int.TryParse(txtY3W31.Text, out y1) ||
                            !int.TryParse(txtX3W32.Text, out x2) || !int.TryParse(txtY3W32.Text, out y2))
                        {
                            txtStatus.Text = "3窓3の座標が設定されていません";
                            return;
                        }
                        break;
                }

                // 指定された窓の色を取得
                Color color1 = ColorDetector.GetColorAt(x1, y1);
                Color color2 = ColorDetector.GetColorAt(x2, y2);

                // RGB最小値を判定
                string lowest1 = GetLowestRGB(color1);
                string lowest2 = GetLowestRGB(color2);
                
                // 使用するルールタブを表示
                string tabPrefix = GetTabPrefixByWeek(currentWeek);

                // ルールと照合
                string result = CheckRules(lowest1, lowest2, color1, color2, windowNumber);

                // 結果を表示（すべてのウィンドウに）
                if (!string.IsNullOrEmpty(result))
                {
                    foreach (var overlayWindow in overlayWindows)
                    {
                        overlayWindow.ShowResultText(result);
                    }

                    // 週を自動的に更新（1-16でループ）
                    currentWeek++;
                    if (currentWeek > 16)
                    {
                        currentWeek = 1;
                    }
                    txtWeek.Text = ConvertWeekToText(currentWeek);
                    foreach (var overlayWindow in overlayWindows)
                    {
                        overlayWindow.SetWeekInfo(currentWeek);
                    }
                    
                    string windowName = windowNumber switch { 0 => "単窓", 4 => "3窓1", 5 => "3窓2", 6 => "3窓3", _ => $"8窓{windowNumber}" };
                    txtStatus.Text = $"{windowName}: {result} ({tabPrefix}ルール使用, 色1:{lowest1}, 色2:{lowest2})";
                }
                else
                {
                    string windowName2 = windowNumber switch { 0 => "単窓", 4 => "3窓1", 5 => "3窓2", 6 => "3窓3", _ => $"8窓{windowNumber}" };
                    txtStatus.Text = $"{windowName2}: ルール不一致 ({tabPrefix}ルール, 色1:{lowest1}, 色2:{lowest2})";
                }
            }
            catch (Exception ex)
            {
                txtStatus.Text = $"エラー: {ex.Message}";
            }
        }

        private string GetLowestRGB(Color color)
        {
            if (color.R < color.G && color.R < color.B)
                return "Da";  // R (Da)
            else if (color.G < color.R && color.G < color.B)
                return "Vo";  // G (Vo)
            else if (color.B < color.R && color.B < color.G)
                return "Vi";  // B (Vi)
            else
                return "不明";
        }

        private string CheckRules(string lowest1, string lowest2, Color color1, Color color2, int window)
        {
            // 現在選択されているタブのルールを使用
            var rules = GetCurrentRuleSet();
            
            // ルール1チェック
            if (lowest1 == rules.Rule1_Coord1RGB && lowest2 == rules.Rule1_Coord2RGB)
            {
                return GetDisplayText(rules.Rule1_Display1, rules.Rule1_Display2);
            }

            // ルール2チェック
            if (lowest1 == rules.Rule2_Coord1RGB && lowest2 == rules.Rule2_Coord2RGB)
            {
                return GetDisplayText(rules.Rule2_Display1, rules.Rule2_Display2);
            }

            // ルール3チェック
            if (lowest1 == rules.Rule3_Coord1RGB && lowest2 == rules.Rule3_Coord2RGB)
            {
                return GetDisplayText(rules.Rule3_Display1, rules.Rule3_Display2);
            }

            // ルール4チェック
            if (lowest1 == rules.Rule4_Coord1RGB && lowest2 == rules.Rule4_Coord2RGB)
            {
                return GetDisplayText(rules.Rule4_Display1, rules.Rule4_Display2);
            }

            // ルール5チェック
            if (lowest1 == rules.Rule5_Coord1RGB && lowest2 == rules.Rule5_Coord2RGB)
            {
                return GetDisplayText(rules.Rule5_Display1, rules.Rule5_Display2);
            }

            // ルール6チェック
            if (lowest1 == rules.Rule6_Coord1RGB && lowest2 == rules.Rule6_Coord2RGB)
            {
                return GetDisplayText(rules.Rule6_Display1, rules.Rule6_Display2);
            }

            return "";
        }

        private RuleSet GetCurrentRuleSet()
        {
            // 週に応じて適切なタブのルールを使用
            // 週1: spotlight, 週2-8: 踊っていいとも, 週9: legend, 週10-12: 七彩, 週13-16: 歌姫
            string tabPrefix = GetTabPrefixByWeek(currentWeek);
            
            var ruleSet = new RuleSet();
            
            try
            {
                // ルール1: VoDaVi (Vo が1位、Da が2位)
                ruleSet.Rule1_Coord1RGB = "Vo";  // 座標1で1位（最小値）
                ruleSet.Rule1_Coord2RGB = "Da";  // 座標2で2位（最小値）
                ruleSet.Rule1_Display1 = GetComboBoxValue($"cmb{tabPrefix}Rule1Display1", "Vo");
                ruleSet.Rule1_Display2 = GetComboBoxValue($"cmb{tabPrefix}Rule1Display2", "Vo");

                // ルール2: VoViDa (Vo が1位、Vi が2位)
                ruleSet.Rule2_Coord1RGB = "Vo";  // 座標1で1位（最小値）
                ruleSet.Rule2_Coord2RGB = "Vi";  // 座標2で2位（最小値）
                ruleSet.Rule2_Display1 = GetComboBoxValue($"cmb{tabPrefix}Rule2Display1", "Da");
                ruleSet.Rule2_Display2 = GetComboBoxValue($"cmb{tabPrefix}Rule2Display2", "Da");

                // ルール3: DaVoVi (Da が1位、Vo が2位)
                ruleSet.Rule3_Coord1RGB = "Da";  // 座標1で1位（最小値）
                ruleSet.Rule3_Coord2RGB = "Vo";  // 座標2で2位（最小値）
                ruleSet.Rule3_Display1 = GetComboBoxValue($"cmb{tabPrefix}Rule3Display1", "Vi");
                ruleSet.Rule3_Display2 = GetComboBoxValue($"cmb{tabPrefix}Rule3Display2", "Vi");

                // ルール4: DaViVo (Da が1位、Vi が2位)
                ruleSet.Rule4_Coord1RGB = "Da";  // 座標1で1位（最小値）
                ruleSet.Rule4_Coord2RGB = "Vi";  // 座標2で2位（最小値）
                ruleSet.Rule4_Display1 = GetComboBoxValue($"cmb{tabPrefix}Rule4Display1", "Vi");
                ruleSet.Rule4_Display2 = GetComboBoxValue($"cmb{tabPrefix}Rule4Display2", "Da");

                // ルール5: ViVoDa (Vi が1位、Vo が2位)
                ruleSet.Rule5_Coord1RGB = "Vi";  // 座標1で1位（最小値）
                ruleSet.Rule5_Coord2RGB = "Vo";  // 座標2で2位（最小値）
                ruleSet.Rule5_Display1 = GetComboBoxValue($"cmb{tabPrefix}Rule5Display1", "Da");
                ruleSet.Rule5_Display2 = GetComboBoxValue($"cmb{tabPrefix}Rule5Display2", "Vi");

                // ルール6: ViDaVo (Vi が1位、Da が2位)
                ruleSet.Rule6_Coord1RGB = "Vi";  // 座標1で1位（最小値）
                ruleSet.Rule6_Coord2RGB = "Da";  // 座標2で2位（最小値）
                ruleSet.Rule6_Display1 = GetComboBoxValue($"cmb{tabPrefix}Rule6Display1", "Vo");
                ruleSet.Rule6_Display2 = GetComboBoxValue($"cmb{tabPrefix}Rule6Display2", "Vi");
            }
            catch
            {
                // エラー時はデフォルト値を使用
            }

            return ruleSet;
        }

        // 週に応じたタブのプレフィックスを取得（アクティブ窓の進行設定を使用）
        private string GetTabPrefixByWeek(int week)
        {
            return GetTabPrefixByWeekForWindow(week, activeWindow);
        }

        private string GetTabPrefixByWeekForWindow(int week, int windowNumber)
        {
            var settings = GetWindowProgression(windowNumber);
            var progression = isYamaAri ? settings.YamaAriProgression : settings.YamaNashiProgression;
            if (progression.TryGetValue(week, out var tag) && !string.IsNullOrEmpty(tag))
                return tag;

            // デフォルト（山あり進行）
            return week switch
            {
                1 => "Spotlight",
                >= 2 and <= 8 => "Odotte",
                9 => "Legend",
                >= 10 and <= 12 => "Nanasai",
                >= 13 and <= 16 => "Utahime",
                _ => "Spotlight"
            };
        }

        private void BtnToggleYama_Click(object sender, RoutedEventArgs e)
        {
            isYamaAri = !isYamaAri;
            UpdateYamaModeDisplay();
        }

        private void UpdateYamaModeDisplay()
        {
            if (isYamaAri)
            {
                btnToggleYama.Content = "山あり";
                btnToggleYama.Background = new SolidColorBrush(Colors.LimeGreen);
                txtYamaModeStatus.Text = "山あり";
                txtYamaModeStatus.Foreground = new SolidColorBrush(Colors.Green);
            }
            else
            {
                btnToggleYama.Content = "山なし";
                btnToggleYama.Background = new SolidColorBrush(Colors.Orange);
                txtYamaModeStatus.Text = "山なし";
                txtYamaModeStatus.Foreground = new SolidColorBrush(Colors.OrangeRed);
            }

            foreach (var overlayWindow in overlayWindows)
            {
                overlayWindow.SetYamaMode(isYamaAri);
            }
        }

        private HashSet<int> GetBomberWeeksFromCheckBoxes()
        {
            var weeks = new HashSet<int>();
            foreach (var kvp in bomberCheckBoxes)
            {
                if (kvp.Value.IsChecked == true)
                    weeks.Add(kvp.Key);
            }
            return weeks;
        }

        private static readonly string[] AuditionTypes = ["えれぇベスト", "spotlight", "踊っていいとも", "legend", "七彩", "歌姫"];
        private static readonly string[] AuditionTags = ["EreeBest", "Spotlight", "Odotte", "Legend", "Nanasai", "Utahime"];

        private static readonly Dictionary<int, int> DefaultYamaAri = new()
        {
            {1, 1}, {2, 2}, {3, 2}, {4, 2}, {5, 2}, {6, 2}, {7, 2}, {8, 2},
            {9, 3}, {10, 4}, {11, 4}, {12, 4}, {13, 5}, {14, 5}, {15, 5}, {16, 5}
        };

        private void InitializeProgressionComboBoxes()
        {
            // すべての窓に対してデフォルト設定を作成
            foreach (int wn in new[] { 0, 1, 2, 3, 4, 5, 6 })
            {
                if (!windowProgressions.ContainsKey(wn))
                    windowProgressions[wn] = new WindowProgressionSettings();
            }
            // 最初の窓（8窓1）のUIを構築
            currentProgressionWindow = 1;
            RebuildProgressionUI();
        }

        private WindowProgressionSettings GetWindowProgression(int windowNumber)
        {
            if (windowProgressions.TryGetValue(windowNumber, out var settings))
                return settings;
            var def = new WindowProgressionSettings();
            windowProgressions[windowNumber] = def;
            return def;
        }

        private void SaveCurrentProgressionToDict()
        {
            var settings = GetWindowProgression(currentProgressionWindow);
            settings.YamaAriProgression = GetProgressionFromComboBoxes(yamaAriComboBoxes);
            settings.YamaNashiProgression = GetProgressionFromComboBoxes(yamaNashiComboBoxes);
            settings.BomberWeeks = GetBomberWeeksFromCheckBoxes().ToList();
        }

        private void RebuildProgressionUI()
        {
            yamaAriComboBoxes.Clear();
            yamaNashiComboBoxes.Clear();
            bomberCheckBoxes.Clear();
            yamaAriContainer.Children.Clear();
            yamaNashiContainer.Children.Clear();
            bomberCheckContainer.Children.Clear();

            var settings = GetWindowProgression(currentProgressionWindow);
            bomberWeeks = new HashSet<int>(settings.BomberWeeks);

            BuildBomberCheckBoxes();
            BuildProgressionGrid(yamaAriContainer, yamaAriComboBoxes, DefaultYamaAri);
            BuildProgressionGrid(yamaNashiContainer, yamaNashiComboBoxes, DefaultYamaAri);

            LoadProgressionComboBoxes(yamaAriComboBoxes, settings.YamaAriProgression);
            LoadProgressionComboBoxes(yamaNashiComboBoxes, settings.YamaNashiProgression);
        }

        private void CmbProgressionWindow_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
           if (yamaAriContainer == null) return; // InitializeComponent() 完了前は処理しない
            if (cmbProgressionWindow.SelectedItem is not ComboBoxItem item) return;
            if (!int.TryParse(item.Tag?.ToString(), out int wn)) return;
            if (yamaAriComboBoxes.Count > 0)
                SaveCurrentProgressionToDict();
            currentProgressionWindow = wn;
            RebuildProgressionUI();
        }

        private void BtnCopyProgressionToAll_Click(object sender, RoutedEventArgs e)
        {
            SaveCurrentProgressionToDict();
            var src = GetWindowProgression(currentProgressionWindow);
            foreach (int wn in new[] { 0, 1, 2, 3, 4, 5, 6 })
            {
                if (wn == currentProgressionWindow) continue;
                windowProgressions[wn] = new WindowProgressionSettings
                {
                    YamaAriProgression = new Dictionary<int, string>(src.YamaAriProgression),
                    YamaNashiProgression = new Dictionary<int, string>(src.YamaNashiProgression),
                    BomberWeeks = new List<int>(src.BomberWeeks)
                };
            }
            txtStatus.Text = "現在の窓の進行設定を全窓にコピーしました";
        }

        private void BuildBomberCheckBoxes()
        {
            for (int w = 1; w <= 16; w++)
            {
                var chk = new CheckBox
                {
                    Content = WeekHelper.ToLabel(w),
                    IsChecked = bomberWeeks.Contains(w),
                    Margin = new Thickness(5, 2, 5, 2),
                    Tag = w
                };
                bomberCheckBoxes[w] = chk;
                bomberCheckContainer.Children.Add(chk);
            }
        }

        private void BuildProgressionGrid(StackPanel container, Dictionary<int, ComboBox> comboBoxes, Dictionary<int, int> defaults)
        {
            var grid = new System.Windows.Controls.Primitives.UniformGrid { Columns = 2 };
            for (int w = 1; w <= 16; w++)
            {
                var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(2) };
                sp.Children.Add(new TextBlock { Text = $"{WeekHelper.ToLabel(w)}:", Width = 30, VerticalAlignment = VerticalAlignment.Center });
                var cmb = new ComboBox { Width = 120, Tag = w };
                for (int i = 0; i < AuditionTypes.Length; i++)
                {
                    cmb.Items.Add(new ComboBoxItem { Content = AuditionTypes[i], Tag = AuditionTags[i] });
                }
                int defaultIdx = defaults.TryGetValue(w, out int idx) ? idx : 0;
                cmb.SelectedIndex = defaultIdx;
                comboBoxes[w] = cmb;
                sp.Children.Add(cmb);
                grid.Children.Add(sp);
            }
            container.Children.Add(grid);
        }

        // コントロール名からComboBoxの値を取得
        private string GetComboBoxValue(string controlName, string defaultValue)
        {
            try
            {
                var control = this.FindName(controlName) as System.Windows.Controls.ComboBox;
                string content = ((ComboBoxItem)control?.SelectedItem)?.Content?.ToString() ?? defaultValue;
                
                // "R (Da)" のような形式の場合、最初の文字だけを取得
                if (content.Contains("("))
                {
                    return content.Split(' ')[0];
                }
                return content;
            }
            catch
            {
                return defaultValue;
            }
        }

        private string GetDisplayText(string type1, string type2)
        {
            string name1 = GetCustomDisplayName(type1);
            string name2 = GetCustomDisplayName(type2);
            return $"{name1}→{name2}";
        }

        private string GetCustomDisplayName(string type)
        {
            return type switch
            {
                "Vo" => "Vo",
                "Da" => "Da",
                "Vi" => "Vi",
                _ => type
            };
        }

        private void RegisterColors(int windowNumber)
        {
            try
            {
                int x1, y1, x2, y2;
                
                switch (windowNumber)
                {
                    case 1:
                        if (!int.TryParse(txtX11.Text, out x1) || !int.TryParse(txtY11.Text, out y1) ||
                            !int.TryParse(txtX12.Text, out x2) || !int.TryParse(txtY12.Text, out y2))
                            return;
                        
                        window1_color1 = ColorDetector.GetColorAt(x1, y1);
                        window1_color2 = ColorDetector.GetColorAt(x2, y2);
                        color1_registered = true;
                        txtStatus.Text = $"窓1の色を登録しました (座標1: {x1},{y1} / 座標2: {x2},{y2})";
                        break;
                        
                    case 2:
                        if (!int.TryParse(txtX21.Text, out x1) || !int.TryParse(txtY21.Text, out y1) ||
                            !int.TryParse(txtX22.Text, out x2) || !int.TryParse(txtY22.Text, out y2))
                            return;
                        
                        window2_color1 = ColorDetector.GetColorAt(x1, y1);
                        window2_color2 = ColorDetector.GetColorAt(x2, y2);
                        color2_registered = true;
                        txtStatus.Text = $"窓2の色を登録しました (座標1: {x1},{y1} / 座標2: {x2},{y2})";
                        break;
                        
                    case 3:
                        if (!int.TryParse(txtX31.Text, out x1) || !int.TryParse(txtY31.Text, out y1) ||
                            !int.TryParse(txtX32.Text, out x2) || !int.TryParse(txtY32.Text, out y2))
                            return;
                        
                        window3_color1 = ColorDetector.GetColorAt(x1, y1);
                        window3_color2 = ColorDetector.GetColorAt(x2, y2);
                        color3_registered = true;
                        txtStatus.Text = $"窓3の色を登録しました (座標1: {x1},{y1} / 座標2: {x2},{y2})";
                        break;
                }
            }
            catch (Exception ex)
            {
                txtStatus.Text = $"エラー: {ex.Message}";
            }
        }

        private void DetectionTimer_Tick(object? sender, EventArgs e)
        {
            if (overlayWindows.Count == 0)
                return;

            try
            {
                // 各窓の2座標を取得
                if (!int.TryParse(txtX11.Text, out int x11) || !int.TryParse(txtY11.Text, out int y11) ||
                    !int.TryParse(txtX12.Text, out int x12) || !int.TryParse(txtY12.Text, out int y12) ||
                    !int.TryParse(txtX21.Text, out int x21) || !int.TryParse(txtY21.Text, out int y21) ||
                    !int.TryParse(txtX22.Text, out int x22) || !int.TryParse(txtY22.Text, out int y22) ||
                    !int.TryParse(txtX31.Text, out int x31) || !int.TryParse(txtY31.Text, out int y31) ||
                    !int.TryParse(txtX32.Text, out int x32) || !int.TryParse(txtY32.Text, out int y32))
                {
                    return;
                }

                // 各窓の現在の色を取得（2座標）
                Color current11 = ColorDetector.GetColorAt(x11, y11);
                Color current12 = ColorDetector.GetColorAt(x12, y12);
                Color current21 = ColorDetector.GetColorAt(x21, y21);
                Color current22 = ColorDetector.GetColorAt(x22, y22);
                Color current31 = ColorDetector.GetColorAt(x31, y31);
                Color current32 = ColorDetector.GetColorAt(x32, y32);

                // 各窓で登録された色とマッチするかチェック
                bool match1 = color1_registered && 
                    (ColorDetector.IsColorMatch(current11, window1_color1, 30) ||
                     ColorDetector.IsColorMatch(current12, window1_color2, 30));
                     
                bool match2 = color2_registered && 
                    (ColorDetector.IsColorMatch(current21, window2_color1, 30) ||
                     ColorDetector.IsColorMatch(current22, window2_color2, 30));
                     
                bool match3 = color3_registered && 
                    (ColorDetector.IsColorMatch(current31, window3_color1, 30) ||
                     ColorDetector.IsColorMatch(current32, window3_color2, 30));

                // マッチした色のRGB値を集める
                List<Color> matchedColors = new List<Color>();
                if (match1)
                {
                    if (ColorDetector.IsColorMatch(current11, window1_color1, 30)) matchedColors.Add(current11);
                    if (ColorDetector.IsColorMatch(current12, window1_color2, 30)) matchedColors.Add(current12);
                }
                if (match2)
                {
                    if (ColorDetector.IsColorMatch(current21, window2_color1, 30)) matchedColors.Add(current21);
                    if (ColorDetector.IsColorMatch(current22, window2_color2, 30)) matchedColors.Add(current22);
                }
                if (match3)
                {
                    if (ColorDetector.IsColorMatch(current31, window3_color1, 30)) matchedColors.Add(current31);
                    if (ColorDetector.IsColorMatch(current32, window3_color2, 30)) matchedColors.Add(current32);
                }

                // すべて非表示にする
                foreach (var overlayWindow in overlayWindows)
                {
                    overlayWindow.HideAll();
                }


                if (matchedColors.Count > 0)
                {
                    // RGB値の最小値を判定（透過ウィンドウでは使用しない）
                    // このコードはShowResultTextで置き換えられました
                }
            }
            catch (Exception ex)
            {
                txtStatus.Text = $"エラー: {ex.Message}";
            }
        }

        private void BtnLaunch_Click(object sender, RoutedEventArgs e)
        {
            // 既存のウィンドウをすべて閉じる
            foreach (var window in overlayWindows.ToList())
            {
                window.Close();
            }
            overlayWindows.Clear();
            
            // 選択された枚数のウィンドウを作成
            int windowCount = cmbWindowCount.SelectedIndex + 1;
            
            for (int i = 0; i < windowCount; i++)
            {
                var overlayWindow = new OverlayWindow();
                overlayWindow.Title = $"表示ウィンドウ {i + 1}";
                overlayWindow.Closed += OverlayWindow_Closed;
                
                // ウィンドウの位置をずらす
                overlayWindow.Left = 100 + (i * 50);
                overlayWindow.Top = 100 + (i * 50);
                
                overlayWindow.Show();
                
                // 現在の週情報とフォントサイズを設定
                var activeBomber = new HashSet<int>(GetWindowProgression(activeWindow).BomberWeeks);
                overlayWindow.SetBomberWeeks(activeBomber);
                overlayWindow.SetWeekInfo(currentWeek);
                overlayWindow.SetYamaMode(isYamaAri);
                if (int.TryParse(txtFontSize.Text, out int fontSize) && fontSize > 0)
                {
                    overlayWindow.SetFontSize(fontSize);
                }
                
                overlayWindows.Add(overlayWindow);
            }
            
            btnLaunch.IsEnabled = false;
            txtStatus.Text = $"表示ウィンドウを{windowCount}枚起動しました";
        }

        private void OverlayWindow_Closed(object? sender, EventArgs e)
        {
            // ウィンドウが閉じられたときの処理
            if (sender is OverlayWindow closedWindow)
            {
                closedWindow.Closed -= OverlayWindow_Closed;
                overlayWindows.Remove(closedWindow);
            }
            
            if (overlayWindows.Count == 0)
            {
                btnLaunch.IsEnabled = true;
                txtStatus.Text = "すべての表示ウィンドウが閉じられました";
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            detectionTimer.Stop();
            keyboardHook?.Stop();
            keyboardHook?.Dispose();
            
            // オーバーレイウィンドウをすべて閉じる
            foreach (var window in overlayWindows.ToList())
            {
                window.Close();
            }
            overlayWindows.Clear();
            
            this.Close();
        }

        private void BtnWeekBack_Click(object sender, RoutedEventArgs e)
        {
            currentWeek--;
            if (currentWeek < 1)
            {
                currentWeek = 16;
            }
            txtWeek.Text = ConvertWeekToText(currentWeek);
            
            foreach (var overlayWindow in overlayWindows)
            {
                overlayWindow.SetWeekInfo(currentWeek);
            }
            
            txtStatus.Text = $"{ConvertWeekToText(currentWeek)}に戻しました";
        }

        private void BtnWeekForward_Click(object sender, RoutedEventArgs e)
        {
            currentWeek++;
            if (currentWeek > 16)
            {
                currentWeek = 1;
            }
            txtWeek.Text = ConvertWeekToText(currentWeek);
            
            foreach (var overlayWindow in overlayWindows)
            {
                overlayWindow.SetWeekInfo(currentWeek);
            }
            
            txtStatus.Text = $"{ConvertWeekToText(currentWeek)}に進めました";
        }

        private void BtnSaveSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settings = new CoordinateSettings
                {
                    Window1_X1 = int.Parse(txtX11.Text),
                    Window1_Y1 = int.Parse(txtY11.Text),
                    Window1_X2 = int.Parse(txtX12.Text),
                    Window1_Y2 = int.Parse(txtY12.Text),

                    Window2_X1 = int.Parse(txtX21.Text),
                    Window2_Y1 = int.Parse(txtY21.Text),
                    Window2_X2 = int.Parse(txtX22.Text),
                    Window2_Y2 = int.Parse(txtY22.Text),

                    Window3_X1 = int.Parse(txtX31.Text),
                    Window3_Y1 = int.Parse(txtY31.Text),
                    Window3_X2 = int.Parse(txtX32.Text),
                    Window3_Y2 = int.Parse(txtY32.Text),

                    WindowSingle_X1 = int.Parse(txtXS1.Text),
                    WindowSingle_Y1 = int.Parse(txtYS1.Text),
                    WindowSingle_X2 = int.Parse(txtXS2.Text),
                    WindowSingle_Y2 = int.Parse(txtYS2.Text),

                    Window3W1_X1 = int.Parse(txtX3W11.Text),
                    Window3W1_Y1 = int.Parse(txtY3W11.Text),
                    Window3W1_X2 = int.Parse(txtX3W12.Text),
                    Window3W1_Y2 = int.Parse(txtY3W12.Text),

                    Window3W2_X1 = int.Parse(txtX3W21.Text),
                    Window3W2_Y1 = int.Parse(txtY3W21.Text),
                    Window3W2_X2 = int.Parse(txtX3W22.Text),
                    Window3W2_Y2 = int.Parse(txtY3W22.Text),

                    Window3W3_X1 = int.Parse(txtX3W31.Text),
                    Window3W3_Y1 = int.Parse(txtY3W31.Text),
                    Window3W3_X2 = int.Parse(txtX3W32.Text),
                    Window3W3_Y2 = int.Parse(txtY3W32.Text),

                    CurrentWeek = currentWeek,
                    FontSize = int.TryParse(txtFontSize.Text, out int fontSize) ? fontSize : 200,
                    
                    JudgeKey = txtJudgeKey.Text,
                    MoveToWindow1Key = txtMoveToWindow1Key.Text,
                    MoveToWindow2Key = txtMoveToWindow2Key.Text,
                    MoveToWindow3Key = txtMoveToWindow3Key.Text,
                    MoveToWindowSingleKey = txtMoveToWindowSKey.Text,
                    MoveToWindow3W1Key = txtMoveToWindow3W1Key.Text,
                    MoveToWindow3W2Key = txtMoveToWindow3W2Key.Text,
                    MoveToWindow3W3Key = txtMoveToWindow3W3Key.Text,
                    
                    // 定点クリック設定
                    AutoClickEnabled = chkAutoClickEnabled.IsChecked == true,
                    CoordRegKey = txtCoordRegKey.Text
                };
                
                // 定点クリック座標を保存
                SavePresetSettings(settings);

                // 各タブのルール設定を保存
                SaveTabRuleSettings(settings);

                settings.SaveToFile(SettingsFilePath);
                string fullPath = System.IO.Path.GetFullPath(SettingsFilePath);
                txtStatus.Text = $"設定を保存しました: {fullPath}";
            }
            catch (Exception ex)
            {
                txtStatus.Text = $"保存エラー: {ex.Message}";
            }
        }
        
        private void SavePresetSettings(CoordinateSettings settings)
        {
            settings.ClickPresets = clickPresets.ToList();
            settings.ClickPatterns = clickPatterns.ToList();
            settings.IsYamaAri = isYamaAri;
            settings.YamaToggleKey = txtYamaToggleKey.Text;

            // 現在表示中の窓の設定を保存してからまとめて書き出す
            SaveCurrentProgressionToDict();
            settings.WindowProgressions = new Dictionary<int, WindowProgressionSettings>(windowProgressions);

            // 旧互換用にも書き出し
            var defaultProg = GetWindowProgression(1);
            settings.BomberWeeks = new List<int>(defaultProg.BomberWeeks);
            settings.YamaAriProgression = new Dictionary<int, string>(defaultProg.YamaAriProgression);
            settings.YamaNashiProgression = new Dictionary<int, string>(defaultProg.YamaNashiProgression);
        }

        private Dictionary<int, string> GetProgressionFromComboBoxes(Dictionary<int, ComboBox> comboBoxes)
        {
            var result = new Dictionary<int, string>();
            foreach (var kvp in comboBoxes)
            {
                if (kvp.Value.SelectedItem is ComboBoxItem item)
                    result[kvp.Key] = item.Tag?.ToString() ?? "Spotlight";
            }
            return result;
        }

        private void SaveTabRuleSettings(CoordinateSettings settings)
        {
            // EreeBest
            settings.EreeBestRules.Rule1_Display1 = GetComboBoxValue(cmbEreeBestRule1Display1);
            settings.EreeBestRules.Rule1_Display2 = GetComboBoxValue(cmbEreeBestRule1Display2);
            settings.EreeBestRules.Rule2_Display1 = GetComboBoxValue(cmbEreeBestRule2Display1);
            settings.EreeBestRules.Rule2_Display2 = GetComboBoxValue(cmbEreeBestRule2Display2);
            settings.EreeBestRules.Rule3_Display1 = GetComboBoxValue(cmbEreeBestRule3Display1);
            settings.EreeBestRules.Rule3_Display2 = GetComboBoxValue(cmbEreeBestRule3Display2);
            settings.EreeBestRules.Rule4_Display1 = GetComboBoxValue(cmbEreeBestRule4Display1);
            settings.EreeBestRules.Rule4_Display2 = GetComboBoxValue(cmbEreeBestRule4Display2);
            settings.EreeBestRules.Rule5_Display1 = GetComboBoxValue(cmbEreeBestRule5Display1);
            settings.EreeBestRules.Rule5_Display2 = GetComboBoxValue(cmbEreeBestRule5Display2);
            settings.EreeBestRules.Rule6_Display1 = GetComboBoxValue(cmbEreeBestRule6Display1);
            settings.EreeBestRules.Rule6_Display2 = GetComboBoxValue(cmbEreeBestRule6Display2);

            // Spotlight
            settings.SpotlightRules.Rule1_Display1 = GetComboBoxValue(cmbSpotlightRule1Display1);
            settings.SpotlightRules.Rule1_Display2 = GetComboBoxValue(cmbSpotlightRule1Display2);
            settings.SpotlightRules.Rule2_Display1 = GetComboBoxValue(cmbSpotlightRule2Display1);
            settings.SpotlightRules.Rule2_Display2 = GetComboBoxValue(cmbSpotlightRule2Display2);
            settings.SpotlightRules.Rule3_Display1 = GetComboBoxValue(cmbSpotlightRule3Display1);
            settings.SpotlightRules.Rule3_Display2 = GetComboBoxValue(cmbSpotlightRule3Display2);
            settings.SpotlightRules.Rule4_Display1 = GetComboBoxValue(cmbSpotlightRule4Display1);
            settings.SpotlightRules.Rule4_Display2 = GetComboBoxValue(cmbSpotlightRule4Display2);
            settings.SpotlightRules.Rule5_Display1 = GetComboBoxValue(cmbSpotlightRule5Display1);
            settings.SpotlightRules.Rule5_Display2 = GetComboBoxValue(cmbSpotlightRule5Display2);
            settings.SpotlightRules.Rule6_Display1 = GetComboBoxValue(cmbSpotlightRule6Display1);
            settings.SpotlightRules.Rule6_Display2 = GetComboBoxValue(cmbSpotlightRule6Display2);
            
            // Odotte
            settings.OdotteRules.Rule1_Display1 = GetComboBoxValue(cmbOdotteRule1Display1);
            settings.OdotteRules.Rule1_Display2 = GetComboBoxValue(cmbOdotteRule1Display2);
            settings.OdotteRules.Rule2_Display1 = GetComboBoxValue(cmbOdotteRule2Display1);
            settings.OdotteRules.Rule2_Display2 = GetComboBoxValue(cmbOdotteRule2Display2);
            settings.OdotteRules.Rule3_Display1 = GetComboBoxValue(cmbOdotteRule3Display1);
            settings.OdotteRules.Rule3_Display2 = GetComboBoxValue(cmbOdotteRule3Display2);
            settings.OdotteRules.Rule4_Display1 = GetComboBoxValue(cmbOdotteRule4Display1);
            settings.OdotteRules.Rule4_Display2 = GetComboBoxValue(cmbOdotteRule4Display2);
            settings.OdotteRules.Rule5_Display1 = GetComboBoxValue(cmbOdotteRule5Display1);
            settings.OdotteRules.Rule5_Display2 = GetComboBoxValue(cmbOdotteRule5Display2);
            settings.OdotteRules.Rule6_Display1 = GetComboBoxValue(cmbOdotteRule6Display1);
            settings.OdotteRules.Rule6_Display2 = GetComboBoxValue(cmbOdotteRule6Display2);
            
            // Legend
            settings.LegendRules.Rule1_Display1 = GetComboBoxValue(cmbLegendRule1Display1);
            settings.LegendRules.Rule1_Display2 = GetComboBoxValue(cmbLegendRule1Display2);
            settings.LegendRules.Rule2_Display1 = GetComboBoxValue(cmbLegendRule2Display1);
            settings.LegendRules.Rule2_Display2 = GetComboBoxValue(cmbLegendRule2Display2);
            settings.LegendRules.Rule3_Display1 = GetComboBoxValue(cmbLegendRule3Display1);
            settings.LegendRules.Rule3_Display2 = GetComboBoxValue(cmbLegendRule3Display2);
            settings.LegendRules.Rule4_Display1 = GetComboBoxValue(cmbLegendRule4Display1);
            settings.LegendRules.Rule4_Display2 = GetComboBoxValue(cmbLegendRule4Display2);
            settings.LegendRules.Rule5_Display1 = GetComboBoxValue(cmbLegendRule5Display1);
            settings.LegendRules.Rule5_Display2 = GetComboBoxValue(cmbLegendRule5Display2);
            settings.LegendRules.Rule6_Display1 = GetComboBoxValue(cmbLegendRule6Display1);
            settings.LegendRules.Rule6_Display2 = GetComboBoxValue(cmbLegendRule6Display2);
            
            // Nanasai
            settings.NanasaiRules.Rule1_Display1 = GetComboBoxValue(cmbNanasaiRule1Display1);
            settings.NanasaiRules.Rule1_Display2 = GetComboBoxValue(cmbNanasaiRule1Display2);
            settings.NanasaiRules.Rule2_Display1 = GetComboBoxValue(cmbNanasaiRule2Display1);
            settings.NanasaiRules.Rule2_Display2 = GetComboBoxValue(cmbNanasaiRule2Display2);
            settings.NanasaiRules.Rule3_Display1 = GetComboBoxValue(cmbNanasaiRule3Display1);
            settings.NanasaiRules.Rule3_Display2 = GetComboBoxValue(cmbNanasaiRule3Display2);
            settings.NanasaiRules.Rule4_Display1 = GetComboBoxValue(cmbNanasaiRule4Display1);
            settings.NanasaiRules.Rule4_Display2 = GetComboBoxValue(cmbNanasaiRule4Display2);
            settings.NanasaiRules.Rule5_Display1 = GetComboBoxValue(cmbNanasaiRule5Display1);
            settings.NanasaiRules.Rule5_Display2 = GetComboBoxValue(cmbNanasaiRule5Display2);
            settings.NanasaiRules.Rule6_Display1 = GetComboBoxValue(cmbNanasaiRule6Display1);
            settings.NanasaiRules.Rule6_Display2 = GetComboBoxValue(cmbNanasaiRule6Display2);
            
            // Utahime
            settings.UtahimeRules.Rule1_Display1 = GetComboBoxValue(cmbUtahimeRule1Display1);
            settings.UtahimeRules.Rule1_Display2 = GetComboBoxValue(cmbUtahimeRule1Display2);
            settings.UtahimeRules.Rule2_Display1 = GetComboBoxValue(cmbUtahimeRule2Display1);
            settings.UtahimeRules.Rule2_Display2 = GetComboBoxValue(cmbUtahimeRule2Display2);
            settings.UtahimeRules.Rule3_Display1 = GetComboBoxValue(cmbUtahimeRule3Display1);
            settings.UtahimeRules.Rule3_Display2 = GetComboBoxValue(cmbUtahimeRule3Display2);
            settings.UtahimeRules.Rule4_Display1 = GetComboBoxValue(cmbUtahimeRule4Display1);
            settings.UtahimeRules.Rule4_Display2 = GetComboBoxValue(cmbUtahimeRule4Display2);
            settings.UtahimeRules.Rule5_Display1 = GetComboBoxValue(cmbUtahimeRule5Display1);
            settings.UtahimeRules.Rule5_Display2 = GetComboBoxValue(cmbUtahimeRule5Display2);
            settings.UtahimeRules.Rule6_Display1 = GetComboBoxValue(cmbUtahimeRule6Display1);
            settings.UtahimeRules.Rule6_Display2 = GetComboBoxValue(cmbUtahimeRule6Display2);
        }
        
        private string GetComboBoxValue(ComboBox? comboBox)
        {
            if (comboBox == null)
                return "";
            
            // SelectedIndexを使用して値を取得（タブが表示されていなくても動作する）
            return comboBox.SelectedIndex switch
            {
                0 => "Vo",
                1 => "Da",
                2 => "Vi",
                _ => ""
            };
        }

        private void BtnLoadSettings_Click(object sender, RoutedEventArgs e)
        {
            LoadSettings();
            string fullPath = System.IO.Path.GetFullPath(SettingsFilePath);
            txtStatus.Text = $"設定を読み込みました: {fullPath}";
        }

        private void LoadSettings()
        {
            try
            {
                var settings = CoordinateSettings.LoadFromFile(SettingsFilePath);

                txtX11.Text = settings.Window1_X1.ToString();
                txtY11.Text = settings.Window1_Y1.ToString();
                txtX12.Text = settings.Window1_X2.ToString();
                txtY12.Text = settings.Window1_Y2.ToString();

                txtX21.Text = settings.Window2_X1.ToString();
                txtY21.Text = settings.Window2_Y1.ToString();
                txtX22.Text = settings.Window2_X2.ToString();
                txtY22.Text = settings.Window2_Y2.ToString();

                txtX31.Text = settings.Window3_X1.ToString();
                txtY31.Text = settings.Window3_Y1.ToString();
                txtX32.Text = settings.Window3_X2.ToString();
                txtY32.Text = settings.Window3_Y2.ToString();

                txtXS1.Text = settings.WindowSingle_X1.ToString();
                txtYS1.Text = settings.WindowSingle_Y1.ToString();
                txtXS2.Text = settings.WindowSingle_X2.ToString();
                txtYS2.Text = settings.WindowSingle_Y2.ToString();

                txtX3W11.Text = settings.Window3W1_X1.ToString();
                txtY3W11.Text = settings.Window3W1_Y1.ToString();
                txtX3W12.Text = settings.Window3W1_X2.ToString();
                txtY3W12.Text = settings.Window3W1_Y2.ToString();

                txtX3W21.Text = settings.Window3W2_X1.ToString();
                txtY3W21.Text = settings.Window3W2_Y1.ToString();
                txtX3W22.Text = settings.Window3W2_X2.ToString();
                txtY3W22.Text = settings.Window3W2_Y2.ToString();

                txtX3W31.Text = settings.Window3W3_X1.ToString();
                txtY3W31.Text = settings.Window3W3_Y1.ToString();
                txtX3W32.Text = settings.Window3W3_X2.ToString();
                txtY3W32.Text = settings.Window3W3_Y2.ToString();

                currentWeek = settings.CurrentWeek;
                txtWeek.Text = ConvertWeekToText(currentWeek);
                txtFontSize.Text = settings.FontSize.ToString();
                
                txtJudgeKey.Text = settings.JudgeKey ?? "Space";
                txtMoveToWindow1Key.Text = settings.MoveToWindow1Key ?? "Z";
                txtMoveToWindow2Key.Text = settings.MoveToWindow2Key ?? "X";
                txtMoveToWindow3Key.Text = settings.MoveToWindow3Key ?? "C";
                txtMoveToWindowSKey.Text = settings.MoveToWindowSingleKey ?? "";
                txtMoveToWindow3W1Key.Text = settings.MoveToWindow3W1Key ?? "";
                txtMoveToWindow3W2Key.Text = settings.MoveToWindow3W2Key ?? "";
                txtMoveToWindow3W3Key.Text = settings.MoveToWindow3W3Key ?? "";
                
                // 定点クリック設定を読み込み
                chkAutoClickEnabled.IsChecked = settings.AutoClickEnabled;
                txtCoordRegKey.Text = settings.CoordRegKey ?? "R";
                LoadPresetSettings(settings);

                // 各タブのルール設定を読み込み
                LoadTabRuleSettings(settings);
            }
            catch
            {
                // 読み込みエラー時はデフォルト値のまま
            }
        }
        
        private void LoadPresetSettings(CoordinateSettings settings)
        {
            clickPresets.Clear();
            if (settings.ClickPresets != null)
            {
                for (int i = 0; i < settings.ClickPresets.Count; i++)
                {
                    var p = settings.ClickPresets[i];
                    if (string.IsNullOrEmpty(p.Name))
                        p.Name = $"定点{i + 1}";
                    clickPresets.Add(p);
                }
            }

            clickPatterns.Clear();
            if (settings.ClickPatterns != null)
            {
                foreach (var p in settings.ClickPatterns)
                    clickPatterns.Add(p);
            }

            isYamaAri = settings.IsYamaAri;
            txtYamaToggleKey.Text = settings.YamaToggleKey ?? "";
            UpdateYamaModeDisplay();

            // 窓別進行設定読み込み
            windowProgressions.Clear();
            if (settings.WindowProgressions != null && settings.WindowProgressions.Count > 0)
            {
                foreach (var kvp in settings.WindowProgressions)
                    windowProgressions[kvp.Key] = kvp.Value;
            }
            else
            {
                // 旧形式からの互換読み込み（全窓に同じ設定をコピー）
                var legacy = new WindowProgressionSettings
                {
                    BomberWeeks = settings.BomberWeeks ?? new List<int> { 8, 13 },
                    YamaAriProgression = settings.YamaAriProgression ?? new(),
                    YamaNashiProgression = settings.YamaNashiProgression ?? new()
                };
                foreach (int wn in new[] { 0, 1, 2, 3, 4, 5, 6 })
                {
                    windowProgressions[wn] = new WindowProgressionSettings
                    {
                        BomberWeeks = new List<int>(legacy.BomberWeeks),
                        YamaAriProgression = new Dictionary<int, string>(legacy.YamaAriProgression),
                        YamaNashiProgression = new Dictionary<int, string>(legacy.YamaNashiProgression)
                    };
                }
            }

            // 全窓のデフォルト設定を保証
            foreach (int wn in new[] { 0, 1, 2, 3, 4, 5, 6 })
            {
                if (!windowProgressions.ContainsKey(wn))
                    windowProgressions[wn] = new WindowProgressionSettings();
            }

            // UIを現在の窓で再構築
            currentProgressionWindow = 1;
            RebuildProgressionUI();
        }

        private void LoadProgressionComboBoxes(Dictionary<int, ComboBox> comboBoxes, Dictionary<int, string>? progression)
        {
            if (progression == null) return;
            foreach (var kvp in progression)
            {
                if (comboBoxes.TryGetValue(kvp.Key, out var cmb))
                {
                    for (int i = 0; i < cmb.Items.Count; i++)
                    {
                        if (cmb.Items[i] is ComboBoxItem item && item.Tag?.ToString() == kvp.Value)
                        {
                            cmb.SelectedIndex = i;
                            break;
                        }
                    }
                }
            }
        }

        private void LoadTabRuleSettings(CoordinateSettings settings)
        {
            // EreeBest
            if (settings.EreeBestRules != null)
            {
                SetComboBoxSelection(cmbEreeBestRule1Display1, settings.EreeBestRules.Rule1_Display1);
                SetComboBoxSelection(cmbEreeBestRule1Display2, settings.EreeBestRules.Rule1_Display2);
                SetComboBoxSelection(cmbEreeBestRule2Display1, settings.EreeBestRules.Rule2_Display1);
                SetComboBoxSelection(cmbEreeBestRule2Display2, settings.EreeBestRules.Rule2_Display2);
                SetComboBoxSelection(cmbEreeBestRule3Display1, settings.EreeBestRules.Rule3_Display1);
                SetComboBoxSelection(cmbEreeBestRule3Display2, settings.EreeBestRules.Rule3_Display2);
                SetComboBoxSelection(cmbEreeBestRule4Display1, settings.EreeBestRules.Rule4_Display1);
                SetComboBoxSelection(cmbEreeBestRule4Display2, settings.EreeBestRules.Rule4_Display2);
                SetComboBoxSelection(cmbEreeBestRule5Display1, settings.EreeBestRules.Rule5_Display1);
                SetComboBoxSelection(cmbEreeBestRule5Display2, settings.EreeBestRules.Rule5_Display2);
                SetComboBoxSelection(cmbEreeBestRule6Display1, settings.EreeBestRules.Rule6_Display1);
                SetComboBoxSelection(cmbEreeBestRule6Display2, settings.EreeBestRules.Rule6_Display2);
            }

            // Spotlight
            if (settings.SpotlightRules != null)
            {
                SetComboBoxSelection(cmbSpotlightRule1Display1, settings.SpotlightRules.Rule1_Display1);
                SetComboBoxSelection(cmbSpotlightRule1Display2, settings.SpotlightRules.Rule1_Display2);
                SetComboBoxSelection(cmbSpotlightRule2Display1, settings.SpotlightRules.Rule2_Display1);
                SetComboBoxSelection(cmbSpotlightRule2Display2, settings.SpotlightRules.Rule2_Display2);
                SetComboBoxSelection(cmbSpotlightRule3Display1, settings.SpotlightRules.Rule3_Display1);
                SetComboBoxSelection(cmbSpotlightRule3Display2, settings.SpotlightRules.Rule3_Display2);
                SetComboBoxSelection(cmbSpotlightRule4Display1, settings.SpotlightRules.Rule4_Display1);
                SetComboBoxSelection(cmbSpotlightRule4Display2, settings.SpotlightRules.Rule4_Display2);
                SetComboBoxSelection(cmbSpotlightRule5Display1, settings.SpotlightRules.Rule5_Display1);
                SetComboBoxSelection(cmbSpotlightRule5Display2, settings.SpotlightRules.Rule5_Display2);
                SetComboBoxSelection(cmbSpotlightRule6Display1, settings.SpotlightRules.Rule6_Display1);
                SetComboBoxSelection(cmbSpotlightRule6Display2, settings.SpotlightRules.Rule6_Display2);
            }
            
            // Odotte
            if (settings.OdotteRules != null)
            {
                SetComboBoxSelection(cmbOdotteRule1Display1, settings.OdotteRules.Rule1_Display1);
                SetComboBoxSelection(cmbOdotteRule1Display2, settings.OdotteRules.Rule1_Display2);
                SetComboBoxSelection(cmbOdotteRule2Display1, settings.OdotteRules.Rule2_Display1);
                SetComboBoxSelection(cmbOdotteRule2Display2, settings.OdotteRules.Rule2_Display2);
                SetComboBoxSelection(cmbOdotteRule3Display1, settings.OdotteRules.Rule3_Display1);
                SetComboBoxSelection(cmbOdotteRule3Display2, settings.OdotteRules.Rule3_Display2);
                SetComboBoxSelection(cmbOdotteRule4Display1, settings.OdotteRules.Rule4_Display1);
                SetComboBoxSelection(cmbOdotteRule4Display2, settings.OdotteRules.Rule4_Display2);
                SetComboBoxSelection(cmbOdotteRule5Display1, settings.OdotteRules.Rule5_Display1);
                SetComboBoxSelection(cmbOdotteRule5Display2, settings.OdotteRules.Rule5_Display2);
                SetComboBoxSelection(cmbOdotteRule6Display1, settings.OdotteRules.Rule6_Display1);
                SetComboBoxSelection(cmbOdotteRule6Display2, settings.OdotteRules.Rule6_Display2);
            }
            
            // Legend
            if (settings.LegendRules != null)
            {
                SetComboBoxSelection(cmbLegendRule1Display1, settings.LegendRules.Rule1_Display1);
                SetComboBoxSelection(cmbLegendRule1Display2, settings.LegendRules.Rule1_Display2);
                SetComboBoxSelection(cmbLegendRule2Display1, settings.LegendRules.Rule2_Display1);
                SetComboBoxSelection(cmbLegendRule2Display2, settings.LegendRules.Rule2_Display2);
                SetComboBoxSelection(cmbLegendRule3Display1, settings.LegendRules.Rule3_Display1);
                SetComboBoxSelection(cmbLegendRule3Display2, settings.LegendRules.Rule3_Display2);
                SetComboBoxSelection(cmbLegendRule4Display1, settings.LegendRules.Rule4_Display1);
                SetComboBoxSelection(cmbLegendRule4Display2, settings.LegendRules.Rule4_Display2);
                SetComboBoxSelection(cmbLegendRule5Display1, settings.LegendRules.Rule5_Display1);
                SetComboBoxSelection(cmbLegendRule5Display2, settings.LegendRules.Rule5_Display2);
                SetComboBoxSelection(cmbLegendRule6Display1, settings.LegendRules.Rule6_Display1);
                SetComboBoxSelection(cmbLegendRule6Display2, settings.LegendRules.Rule6_Display2);
            }
            
            // Nanasai
            if (settings.NanasaiRules != null)
            {
                SetComboBoxSelection(cmbNanasaiRule1Display1, settings.NanasaiRules.Rule1_Display1);
                SetComboBoxSelection(cmbNanasaiRule1Display2, settings.NanasaiRules.Rule1_Display2);
                SetComboBoxSelection(cmbNanasaiRule2Display1, settings.NanasaiRules.Rule2_Display1);
                SetComboBoxSelection(cmbNanasaiRule2Display2, settings.NanasaiRules.Rule2_Display2);
                SetComboBoxSelection(cmbNanasaiRule3Display1, settings.NanasaiRules.Rule3_Display1);
                SetComboBoxSelection(cmbNanasaiRule3Display2, settings.NanasaiRules.Rule3_Display2);
                SetComboBoxSelection(cmbNanasaiRule4Display1, settings.NanasaiRules.Rule4_Display1);
                SetComboBoxSelection(cmbNanasaiRule4Display2, settings.NanasaiRules.Rule4_Display2);
                SetComboBoxSelection(cmbNanasaiRule5Display1, settings.NanasaiRules.Rule5_Display1);
                SetComboBoxSelection(cmbNanasaiRule5Display2, settings.NanasaiRules.Rule5_Display2);
                SetComboBoxSelection(cmbNanasaiRule6Display1, settings.NanasaiRules.Rule6_Display1);
                SetComboBoxSelection(cmbNanasaiRule6Display2, settings.NanasaiRules.Rule6_Display2);
            }
            
            // Utahime
            if (settings.UtahimeRules != null)
            {
                SetComboBoxSelection(cmbUtahimeRule1Display1, settings.UtahimeRules.Rule1_Display1);
                SetComboBoxSelection(cmbUtahimeRule1Display2, settings.UtahimeRules.Rule1_Display2);
                SetComboBoxSelection(cmbUtahimeRule2Display1, settings.UtahimeRules.Rule2_Display1);
                SetComboBoxSelection(cmbUtahimeRule2Display2, settings.UtahimeRules.Rule2_Display2);
                SetComboBoxSelection(cmbUtahimeRule3Display1, settings.UtahimeRules.Rule3_Display1);
                SetComboBoxSelection(cmbUtahimeRule3Display2, settings.UtahimeRules.Rule3_Display2);
                SetComboBoxSelection(cmbUtahimeRule4Display1, settings.UtahimeRules.Rule4_Display1);
                SetComboBoxSelection(cmbUtahimeRule4Display2, settings.UtahimeRules.Rule4_Display2);
                SetComboBoxSelection(cmbUtahimeRule5Display1, settings.UtahimeRules.Rule5_Display1);
                SetComboBoxSelection(cmbUtahimeRule5Display2, settings.UtahimeRules.Rule5_Display2);
                SetComboBoxSelection(cmbUtahimeRule6Display1, settings.UtahimeRules.Rule6_Display1);
                SetComboBoxSelection(cmbUtahimeRule6Display2, settings.UtahimeRules.Rule6_Display2);
            }
        }
        
        private void SetComboBoxSelection(System.Windows.Controls.ComboBox comboBox, string value)
        {
            if (comboBox == null || string.IsNullOrEmpty(value))
                return;
            
            // SelectedIndexを使用して設定（タブが表示されていなくても動作する）
            int index = value switch
            {
                "Vo" => 0,
                "Da" => 1,
                "Vi" => 2,
                _ => -1
            };
            
            if (index >= 0 && index < comboBox.Items.Count)
            {
                comboBox.SelectedIndex = index;
            }
        }
        
        private string ConvertWeekToText(int week) => WeekHelper.ToLabel(week);
        
        private async Task ExecuteFixedClicks(ClickPattern pattern)
        {
            if (chkAutoClickEnabled == null || chkAutoClickEnabled.IsChecked != true)
                return;

            foreach (var name in pattern.PresetNames)
            {
                var preset = clickPresets.FirstOrDefault(p => p.Name == name);
                if (preset != null)
                {
                    if (preset.Delay > 0)
                        await Task.Delay(preset.Delay);
                    PerformClick(preset.X, preset.Y);
                }
            }
        }

        private void PerformClick(int x, int y)
        {
            SetCursorPos(x, y);
            mouse_event(MOUSEEVENTF_LEFTDOWN, x, y, 0, 0);
            mouse_event(MOUSEEVENTF_LEFTUP, x, y, 0, 0);
        }

        // パターン編集ウィンドウを開く
        private void BtnOpenPatternEditor_Click(object sender, RoutedEventArgs e)
        {
            var editor = new PatternEditorWindow(clickPresets, clickPatterns);
            editor.Owner = this;
            editor.ShowDialog();
        }

        private void RegisterFixedClickCoordinate()
        {
            if (dgPresets.SelectedItem is not ClickPreset preset)
            {
                txtStatus.Text = "定点クリック登録: アクティブな項目がありません";
                return;
            }

            if (!GetCursorPos(out POINT point))
                return;

            preset.X = point.X;
            preset.Y = point.Y;
            txtPresetX.Text = point.X.ToString();
            txtPresetY.Text = point.Y.ToString();
            dgPresets.Items.Refresh();
            txtStatus.Text = $"{preset.Name} 登録: X={point.X}, Y={point.Y}";
        }
    }
}