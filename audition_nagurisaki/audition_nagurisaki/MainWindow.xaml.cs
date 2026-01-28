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

namespace audition_nagurisaki
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer detectionTimer;
        private DispatcherTimer mouseTrackingTimer;
        private List<OverlayWindow> overlayWindows = new List<OverlayWindow>();
        private KeyboardHook? keyboardHook;
        
        private const string SettingsFilePath = "coordinates_settings.json";
        
        private string currentAuditionTab = "Default"; // 現在選択されている情報タブ
        private int currentWeek = 1; // 現在の週（1-16）
        
        // 現在アクティブな窓（1-3）
        private int activeWindow = 1;
        
        // アクティブな座標登録対象（設定タブ用）
        private string? activeCoordTarget = null; // "11", "12", "21", "22", "31", "32"
        
        // アクティブな定点クリック座標登録対象
        private (int window, int index)? activeFixedClickTarget = null;
        
        
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
            this.Title = "審査員殴り";  // ウィンドウタイトルを日本語に設定
            SetJapaneseText();  // UIテキストを日本語に設定
            LoadSettings();  // 起動時に設定を読み込み
            InitializeTimers();
            InitializeKeyboardHook();
        }

        private void TabControlAuditions_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (tabControlAuditions.SelectedItem == tabSpotlight)
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
            grpWindow1.Header = "窓1 識別座標";
            grpWindow2.Header = "窓2 識別座標";
            grpWindow3.Header = "窓3 識別座標";
            
            
            // マウス座標
            lblMousePos.Text = "マウス座標: ";
            
            // 週情報
            grpWeekInfo.Header = "週情報";
            lblWeek.Text = "現在の週:";
            txtWeek.Text = ConvertWeekToText(currentWeek);
            
            
            // 情報タブ
            tabSpotlight.Header = "spotlight";
            tabOdotte.Header = "踊っていいとも";
            tabLegend.Header = "legend";
            tabNanasai.Header = "七彩";
            tabUtahime.Header = "歌姫";
            
            
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

            // マウス座標追跡タイマー
            mouseTrackingTimer = new DispatcherTimer();
            mouseTrackingTimer.Interval = TimeSpan.FromMilliseconds(50);
            mouseTrackingTimer.Tick += MouseTrackingTimer_Tick;
            mouseTrackingTimer.Start(); // 常に実行
        }

        private void InitializeKeyboardHook()
        {
            keyboardHook = new KeyboardHook();
            keyboardHook.KeyPressed += KeyboardHook_KeyPressed;
            keyboardHook.Start(); // アプリ起動時にキーボードフックを開始
        }

        private void KeyboardHook_KeyPressed(object? sender, Key e)
        {
            Dispatcher.Invoke(() =>
            {
                string keyString = e.ToString().ToUpper();
                
                // デバッグ用ログ
                txtStatus.Text = $"キー検出: {keyString}";
                
                // 窓移動キーチェック
                if (KeyMatches(keyString, e, txtMoveToWindow1Key.Text))
                {
                    SetActiveWindow(1);
                    return;
                }
                if (KeyMatches(keyString, e, txtMoveToWindow2Key.Text))
                {
                    SetActiveWindow(2);
                    return;
                }
                if (KeyMatches(keyString, e, txtMoveToWindow3Key.Text))
                {
                    SetActiveWindow(3);
                    return;
                }
                
                // 判別キーチェック（アクティブ窓のみ）
                if (KeyMatches(keyString, e, txtJudgeKey.Text))
                {
                    PerformWindowJudgement(activeWindow);
                    return;
                }
                
                // 週の進む/戻るホットキー
                if (e == Key.Left)
                {
                    BtnWeekBack_Click(null, null);
                    return;
                }
                if (e == Key.Right)
                {
                    BtnWeekForward_Click(null, null);
                    return;
                }
                
                // 座標登録キー（設定タブ用）
                if (KeyMatches(keyString, e, txtCoordRegKey.Text))
                {
                    RegisterCoordinate();
                    return;
                }
                
                // 定点クリック座標登録キー
                if (KeyMatches(keyString, e, txtFixedClickRegKey.Text))
                {
                    RegisterFixedClickCoordinate();
                    return;
                }
            });
        }
        
        private void SetActiveWindow(int windowNumber)
        {
            activeWindow = windowNumber;
            txtCurrentWindow.Text = $"窓{windowNumber}";
            txtStatus.Text = $"窓{windowNumber}に移動しました";
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
                    _ => null
                };
                
                if (txtCoordRegStatus != null && activeCoordTarget != null)
                {
                    int w = int.Parse(activeCoordTarget[0].ToString());
                    int c = int.Parse(activeCoordTarget[1].ToString());
                    txtCoordRegStatus.Text = $"(アクティブ: 窓{w}-座標{c})";
                }
            }
        }
        
        private void FixedClickRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb)
            {
                activeFixedClickTarget = rb.Name switch
                {
                    "rbW1C1" => (1, 1), "rbW1C2" => (1, 2), "rbW1C3" => (1, 3),
                    "rbW1C4" => (1, 4), "rbW1C5" => (1, 5), "rbW1C6" => (1, 6),
                    "rbW1C7" => (1, 7), "rbW1C8" => (1, 8), "rbW1C9" => (1, 9),
                    "rbW2C1" => (2, 1), "rbW2C2" => (2, 2), "rbW2C3" => (2, 3),
                    "rbW2C4" => (2, 4), "rbW2C5" => (2, 5), "rbW2C6" => (2, 6),
                    "rbW2C7" => (2, 7), "rbW2C8" => (2, 8), "rbW2C9" => (2, 9),
                    "rbW3C1" => (3, 1), "rbW3C2" => (3, 2), "rbW3C3" => (3, 3),
                    "rbW3C4" => (3, 4), "rbW3C5" => (3, 5), "rbW3C6" => (3, 6),
                    "rbW3C7" => (3, 7), "rbW3C8" => (3, 8), "rbW3C9" => (3, 9),
                    _ => null
                };
                
                if (txtFixedClickRegStatus != null && activeFixedClickTarget.HasValue)
                {
                    var (w, i) = activeFixedClickTarget.Value;
                    txtFixedClickRegStatus.Text = $"(アクティブ: 窓{w}-{i})";
                }
            }
        }
        
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
                    txtCoordRegStatus.Text = "(アクティブ: なし)";
                }
                
                // 定点クリック座標登録アクティブを解除
                if (activeFixedClickTarget.HasValue)
                {
                    activeFixedClickTarget = null;
                    // すべてのRadioButtonのチェックを解除
                    ClearAllFixedClickRadioButtons();
                    txtFixedClickRegStatus.Text = "(アクティブ: なし)";
                }
            }
        }
        
        private void ClearAllFixedClickRadioButtons()
        {
            rbW1C1.IsChecked = false; rbW1C2.IsChecked = false; rbW1C3.IsChecked = false;
            rbW1C4.IsChecked = false; rbW1C5.IsChecked = false; rbW1C6.IsChecked = false;
            rbW1C7.IsChecked = false; rbW1C8.IsChecked = false; rbW1C9.IsChecked = false;
            rbW2C1.IsChecked = false; rbW2C2.IsChecked = false; rbW2C3.IsChecked = false;
            rbW2C4.IsChecked = false; rbW2C5.IsChecked = false; rbW2C6.IsChecked = false;
            rbW2C7.IsChecked = false; rbW2C8.IsChecked = false; rbW2C9.IsChecked = false;
            rbW3C1.IsChecked = false; rbW3C2.IsChecked = false; rbW3C3.IsChecked = false;
            rbW3C4.IsChecked = false; rbW3C5.IsChecked = false; rbW3C6.IsChecked = false;
            rbW3C7.IsChecked = false; rbW3C8.IsChecked = false; rbW3C9.IsChecked = false;
        }

        private bool KeyMatches(string keyString, Key e, string targetKey)
        {
            string target = targetKey.ToUpper();
            if (keyString == target)
                return true;
            if (e >= Key.D1 && e <= Key.D9 && keyString == "D" + target)
                return true;
            // スペースキー対応
            if (target == "SPACE" && e == Key.Space)
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
            txtStatus.Text = $"PerformWindowJudgement({windowNumber})呼び出し";
            
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
                            txtStatus.Text = "窓1の座標が設定されていません";
                            return;
                        }
                        break;
                    case 2:
                        if (!int.TryParse(txtX21.Text, out x1) || !int.TryParse(txtY21.Text, out y1) ||
                            !int.TryParse(txtX22.Text, out x2) || !int.TryParse(txtY22.Text, out y2))
                        {
                            txtStatus.Text = "窓2の座標が設定されていません";
                            return;
                        }
                        break;
                    case 3:
                        if (!int.TryParse(txtX31.Text, out x1) || !int.TryParse(txtY31.Text, out y1) ||
                            !int.TryParse(txtX32.Text, out x2) || !int.TryParse(txtY32.Text, out y2))
                        {
                            txtStatus.Text = "窓3の座標が設定されていません";
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

                // ルールと照合
                string result = CheckRules(lowest1, lowest2, color1, color2, windowNumber);

                txtStatus.Text = $"判別結果: {result} (色1:{lowest1}, 色2:{lowest2})";

                // 結果を表示（すべてのウィンドウに）
                if (!string.IsNullOrEmpty(result))
                {
                    foreach (var overlayWindow in overlayWindows)
                    {
                        overlayWindow.ShowResultText(result);
                    }
                    
                    // 定点クリックを実行（窓番号に対応したクリック座標を使用）
                    _ = ExecuteFixedClicks(windowNumber);
                    
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
                    
                    txtStatus.Text = $"窓{windowNumber}判別完了: {result} - {ConvertWeekToText(currentWeek)}";
                }
                else
                {
                    txtStatus.Text = $"窓{windowNumber}: ルール不一致";
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
                ruleSet.Rule1_Coord1RGB = GetComboBoxValue($"cmb{tabPrefix}Rule1Coord1RGB", "Da");
                ruleSet.Rule1_Coord2RGB = GetComboBoxValue($"cmb{tabPrefix}Rule1Coord2RGB", "Vo");
                ruleSet.Rule1_Display1 = GetComboBoxValue($"cmb{tabPrefix}Rule1Display1", "Vo");
                ruleSet.Rule1_Display2 = GetComboBoxValue($"cmb{tabPrefix}Rule1Display2", "Vo");

                ruleSet.Rule2_Coord1RGB = GetComboBoxValue($"cmb{tabPrefix}Rule2Coord1RGB", "Vo");
                ruleSet.Rule2_Coord2RGB = GetComboBoxValue($"cmb{tabPrefix}Rule2Coord2RGB", "Da");
                ruleSet.Rule2_Display1 = GetComboBoxValue($"cmb{tabPrefix}Rule2Display1", "Da");
                ruleSet.Rule2_Display2 = GetComboBoxValue($"cmb{tabPrefix}Rule2Display2", "Da");

                ruleSet.Rule3_Coord1RGB = GetComboBoxValue($"cmb{tabPrefix}Rule3Coord1RGB", "Vi");
                ruleSet.Rule3_Coord2RGB = GetComboBoxValue($"cmb{tabPrefix}Rule3Coord2RGB", "Vi");
                ruleSet.Rule3_Display1 = GetComboBoxValue($"cmb{tabPrefix}Rule3Display1", "Vi");
                ruleSet.Rule3_Display2 = GetComboBoxValue($"cmb{tabPrefix}Rule3Display2", "Vi");

                ruleSet.Rule4_Coord1RGB = GetComboBoxValue($"cmb{tabPrefix}Rule4Coord1RGB", "Da");
                ruleSet.Rule4_Coord2RGB = GetComboBoxValue($"cmb{tabPrefix}Rule4Coord2RGB", "Vi");
                ruleSet.Rule4_Display1 = GetComboBoxValue($"cmb{tabPrefix}Rule4Display1", "Vi");
                ruleSet.Rule4_Display2 = GetComboBoxValue($"cmb{tabPrefix}Rule4Display2", "Da");

                ruleSet.Rule5_Coord1RGB = GetComboBoxValue($"cmb{tabPrefix}Rule5Coord1RGB", "Vi");
                ruleSet.Rule5_Coord2RGB = GetComboBoxValue($"cmb{tabPrefix}Rule5Coord2RGB", "Da");
                ruleSet.Rule5_Display1 = GetComboBoxValue($"cmb{tabPrefix}Rule5Display1", "Da");
                ruleSet.Rule5_Display2 = GetComboBoxValue($"cmb{tabPrefix}Rule5Display2", "Vi");

                ruleSet.Rule6_Coord1RGB = GetComboBoxValue($"cmb{tabPrefix}Rule6Coord1RGB", "Vo");
                ruleSet.Rule6_Coord2RGB = GetComboBoxValue($"cmb{tabPrefix}Rule6Coord2RGB", "Vi");
                ruleSet.Rule6_Display1 = GetComboBoxValue($"cmb{tabPrefix}Rule6Display1", "Vo");
                ruleSet.Rule6_Display2 = GetComboBoxValue($"cmb{tabPrefix}Rule6Display2", "Vi");
            }
            catch
            {
                // エラー時はデフォルト値を使用
            }

            return ruleSet;
        }

        // 週に応じたタブのプレフィックスを取得
        private string GetTabPrefixByWeek(int week)
        {
            return week switch
            {
                1 => "Spotlight",           // 週1: spotlight
                >= 2 and <= 8 => "Odotte",  // 週2-8: 踊っていいとも
                9 => "Legend",              // 週9: legend
                >= 10 and <= 12 => "Nanasai", // 週10-12: 七彩
                >= 13 and <= 16 => "Utahime", // 週13-16: 歌姫
                _ => "Spotlight"            // デフォルト
            };
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

        private void MouseTrackingTimer_Tick(object? sender, EventArgs e)
        {
            if (GetCursorPos(out POINT point))
            {
                txtMousePosition.Text = $"X={point.X}, Y={point.Y}";
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
                overlayWindow.SetWeekInfo(currentWeek);
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
            mouseTrackingTimer.Stop();
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

                    CurrentWeek = currentWeek,
                    FontSize = int.TryParse(txtFontSize.Text, out int fontSize) ? fontSize : 200,
                    
                    JudgeKey = txtJudgeKey.Text,
                    MoveToWindow1Key = txtMoveToWindow1Key.Text,
                    MoveToWindow2Key = txtMoveToWindow2Key.Text,
                    MoveToWindow3Key = txtMoveToWindow3Key.Text
                };

                settings.SaveToFile(SettingsFilePath);
                string fullPath = System.IO.Path.GetFullPath(SettingsFilePath);
                txtStatus.Text = $"設定を保存しました: {fullPath}";
            }
            catch (Exception ex)
            {
                txtStatus.Text = $"保存エラー: {ex.Message}";
            }
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

                currentWeek = settings.CurrentWeek;
                txtWeek.Text = ConvertWeekToText(currentWeek);
                txtFontSize.Text = settings.FontSize.ToString();
                
                txtJudgeKey.Text = settings.JudgeKey ?? "Space";
                txtMoveToWindow1Key.Text = settings.MoveToWindow1Key ?? "Z";
                txtMoveToWindow2Key.Text = settings.MoveToWindow2Key ?? "X";
                txtMoveToWindow3Key.Text = settings.MoveToWindow3Key ?? "C";
            }
            catch
            {
                // 読み込みエラー時はデフォルト値のまま
            }
        }
        
        private void SetComboBoxSelection(System.Windows.Controls.ComboBox comboBox, string value)
        {
            foreach (ComboBoxItem item in comboBox.Items)
            {
                if (item.Content?.ToString() == value)
                {
                    comboBox.SelectedItem = item;
                    break;
                }
            }
        }
        
        private string ConvertWeekToText(int week)
        {
            return week switch
            {
                1 => "4-1",
                2 => "3-8",
                3 => "3-7",
                4 => "3-6",
                5 => "3-5",
                6 => "3-4",
                7 => "3-3",
                8 => "3-2",
                9 => "3-1",
                10 => "4-8",
                11 => "4-7",
                12 => "4-6",
                13 => "4-5",
                14 => "4-4",
                15 => "4-3",
                16 => "4-2",
                _ => "4-1"
            };
        }
        
        private async Task ExecuteFixedClicks(int windowNumber)
        {
            // 定点クリックが無効の場合は何もしない
            if (chkAutoClickEnabled == null || chkAutoClickEnabled.IsChecked != true)
                return;

            var clickPoints = new List<(int x, int y, int delay)>();
            
            // 窓番号に応じたチェックボックスとテキストボックスを取得
            CheckBox?[] checkBoxes;
            TextBox?[] xBoxes;
            TextBox?[] yBoxes;
            TextBox?[] delayBoxes;
            
            switch (windowNumber)
            {
                case 1:
                    checkBoxes = new[] { chkW1Click1, chkW1Click2, chkW1Click3, chkW1Click4, chkW1Click5, chkW1Click6, chkW1Click7, chkW1Click8, chkW1Click9 };
                    xBoxes = new[] { txtW1X1, txtW1X2, txtW1X3, txtW1X4, txtW1X5, txtW1X6, txtW1X7, txtW1X8, txtW1X9 };
                    yBoxes = new[] { txtW1Y1, txtW1Y2, txtW1Y3, txtW1Y4, txtW1Y5, txtW1Y6, txtW1Y7, txtW1Y8, txtW1Y9 };
                    delayBoxes = new[] { txtW1D1, txtW1D2, txtW1D3, txtW1D4, txtW1D5, txtW1D6, txtW1D7, txtW1D8, txtW1D9 };
                    break;
                case 2:
                    checkBoxes = new[] { chkW2Click1, chkW2Click2, chkW2Click3, chkW2Click4, chkW2Click5, chkW2Click6, chkW2Click7, chkW2Click8, chkW2Click9 };
                    xBoxes = new[] { txtW2X1, txtW2X2, txtW2X3, txtW2X4, txtW2X5, txtW2X6, txtW2X7, txtW2X8, txtW2X9 };
                    yBoxes = new[] { txtW2Y1, txtW2Y2, txtW2Y3, txtW2Y4, txtW2Y5, txtW2Y6, txtW2Y7, txtW2Y8, txtW2Y9 };
                    delayBoxes = new[] { txtW2D1, txtW2D2, txtW2D3, txtW2D4, txtW2D5, txtW2D6, txtW2D7, txtW2D8, txtW2D9 };
                    break;
                case 3:
                    checkBoxes = new[] { chkW3Click1, chkW3Click2, chkW3Click3, chkW3Click4, chkW3Click5, chkW3Click6, chkW3Click7, chkW3Click8, chkW3Click9 };
                    xBoxes = new[] { txtW3X1, txtW3X2, txtW3X3, txtW3X4, txtW3X5, txtW3X6, txtW3X7, txtW3X8, txtW3X9 };
                    yBoxes = new[] { txtW3Y1, txtW3Y2, txtW3Y3, txtW3Y4, txtW3Y5, txtW3Y6, txtW3Y7, txtW3Y8, txtW3Y9 };
                    delayBoxes = new[] { txtW3D1, txtW3D2, txtW3D3, txtW3D4, txtW3D5, txtW3D6, txtW3D7, txtW3D8, txtW3D9 };
                    break;
                default:
                    return;
            }
            
            // 有効なクリック座標を収集（1から9まで順番に）
            for (int i = 0; i < 9; i++)
            {
                if (checkBoxes[i]?.IsChecked == true &&
                    int.TryParse(xBoxes[i]?.Text, out int x) &&
                    int.TryParse(yBoxes[i]?.Text, out int y) &&
                    int.TryParse(delayBoxes[i]?.Text, out int delay))
                {
                    clickPoints.Add((x, y, delay));
                }
            }

            // 各ポイントを順番にクリック
            foreach (var (x, y, delay) in clickPoints)
            {
                if (delay > 0)
                {
                    await Task.Delay(delay);
                }
                
                PerformClick(x, y);
            }
        }
        
        private void PerformClick(int x, int y)
        {
            // 指定座標に移動してクリック
            SetCursorPos(x, y);
            mouse_event(MOUSEEVENTF_LEFTDOWN, x, y, 0, 0);
            mouse_event(MOUSEEVENTF_LEFTUP, x, y, 0, 0);
        }
        
        private void RegisterFixedClickCoordinate()
        {
            if (!activeFixedClickTarget.HasValue)
            {
                txtStatus.Text = "定点クリック登録: アクティブな項目がありません";
                return;
            }
            
            if (!GetCursorPos(out POINT point))
                return;
            
            var (window, index) = activeFixedClickTarget.Value;
            
            // 現在の窓・座標番号に応じてテキストボックスを取得
            TextBox? xBox = null;
            TextBox? yBox = null;
            CheckBox? checkBox = null;
            
            switch (window)
            {
                case 1:
                    (xBox, yBox, checkBox) = index switch
                    {
                        1 => (txtW1X1, txtW1Y1, chkW1Click1),
                        2 => (txtW1X2, txtW1Y2, chkW1Click2),
                        3 => (txtW1X3, txtW1Y3, chkW1Click3),
                        4 => (txtW1X4, txtW1Y4, chkW1Click4),
                        5 => (txtW1X5, txtW1Y5, chkW1Click5),
                        6 => (txtW1X6, txtW1Y6, chkW1Click6),
                        7 => (txtW1X7, txtW1Y7, chkW1Click7),
                        8 => (txtW1X8, txtW1Y8, chkW1Click8),
                        9 => (txtW1X9, txtW1Y9, chkW1Click9),
                        _ => (null, null, null)
                    };
                    break;
                case 2:
                    (xBox, yBox, checkBox) = index switch
                    {
                        1 => (txtW2X1, txtW2Y1, chkW2Click1),
                        2 => (txtW2X2, txtW2Y2, chkW2Click2),
                        3 => (txtW2X3, txtW2Y3, chkW2Click3),
                        4 => (txtW2X4, txtW2Y4, chkW2Click4),
                        5 => (txtW2X5, txtW2Y5, chkW2Click5),
                        6 => (txtW2X6, txtW2Y6, chkW2Click6),
                        7 => (txtW2X7, txtW2Y7, chkW2Click7),
                        8 => (txtW2X8, txtW2Y8, chkW2Click8),
                        9 => (txtW2X9, txtW2Y9, chkW2Click9),
                        _ => (null, null, null)
                    };
                    break;
                case 3:
                    (xBox, yBox, checkBox) = index switch
                    {
                        1 => (txtW3X1, txtW3Y1, chkW3Click1),
                        2 => (txtW3X2, txtW3Y2, chkW3Click2),
                        3 => (txtW3X3, txtW3Y3, chkW3Click3),
                        4 => (txtW3X4, txtW3Y4, chkW3Click4),
                        5 => (txtW3X5, txtW3Y5, chkW3Click5),
                        6 => (txtW3X6, txtW3Y6, chkW3Click6),
                        7 => (txtW3X7, txtW3Y7, chkW3Click7),
                        8 => (txtW3X8, txtW3Y8, chkW3Click8),
                        9 => (txtW3X9, txtW3Y9, chkW3Click9),
                        _ => (null, null, null)
                    };
                    break;
            }
            
            if (xBox != null && yBox != null)
            {
                xBox.Text = point.X.ToString();
                yBox.Text = point.Y.ToString();
                if (checkBox != null)
                    checkBox.IsChecked = true;
                
                txtStatus.Text = $"定点クリック登録: 窓{window}-{index} X={point.X}, Y={point.Y}";
            }
        }
    }
}