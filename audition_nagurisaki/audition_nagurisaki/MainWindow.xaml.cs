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
        private OverlayWindow? overlayWindow;
        private KeyboardHook? keyboardHook;
        
        private const string SettingsFilePath = "coordinates_settings.json";
        
        private string currentAuditionTab = "Default"; // 現在選択されている情報タブ
        private int currentWeek = 1; // 現在の週（1-16）
        
        
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
            grpWindow1.Header = "窓1";
            grpWindow2.Header = "窓2";
            grpWindow3.Header = "窓3";
            
            // 座標ラベル
            lblCoord1_1.Text = "座標1: X=";
            lblCoord1_2.Text = "座標2: X=";
            lblCoord2_1.Text = "座標1: X=";
            lblCoord2_2.Text = "座標2: X=";
            lblCoord3_1.Text = "座標1: X=";
            lblCoord3_2.Text = "座標2: X=";
            
            // 登録キーラベル
            lblRegKey1_1.Text = " キー:";
            lblRegKey1_2.Text = " キー:";
            lblRegKey2_1.Text = " キー:";
            lblRegKey2_2.Text = " キー:";
            lblRegKey3_1.Text = " キー:";
            lblRegKey3_2.Text = " キー:";
            
            // マウス座標
            lblMousePos.Text = "マウス座標: ";
            
            // 週情報
            grpWeekInfo.Header = "週情報";
            lblWeek.Text = "現在の週:";
            lblJudgeKey.Text = "判別キー:";
            lblJudgeInfo.Text = "(押すと識別と週更新)";
            
            
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
            
            // 表示名カスタマイズ
            grpDisplayNames.Header = "表示名設定";
            lblVoName.Text = "Vo表示名:";
            lblDaName.Text = "Da表示名:";
            lblViName.Text = "Vi表示名:";
            
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
                
                // 判別開始キーチェック
                if (keyString == txtJudgeKey.Text.ToUpper())
                {
                    PerformJudgement();
                    return;
                }
                
                // 座標自動入力キー（現在のマウス座標を取得）
                if (GetCursorPos(out POINT point))
                {
                    // 窓1-座標1の登録キー
                    if (KeyMatches(keyString, e, txtRegKey11.Text))
                    {
                        txtX11.Text = point.X.ToString();
                        txtY11.Text = point.Y.ToString();
                        txtStatus.Text = $"窓1-座標1を設定: X={point.X}, Y={point.Y}";
                        return;
                    }
                    // 窓1-座標2の登録キー
                    else if (KeyMatches(keyString, e, txtRegKey12.Text))
                    {
                        txtX12.Text = point.X.ToString();
                        txtY12.Text = point.Y.ToString();
                        txtStatus.Text = $"窓1-座標2を設定: X={point.X}, Y={point.Y}";
                        return;
                    }
                    // 窓2-座標1の登録キー
                    else if (KeyMatches(keyString, e, txtRegKey21.Text))
                    {
                        txtX21.Text = point.X.ToString();
                        txtY21.Text = point.Y.ToString();
                        txtStatus.Text = $"窓2-座標1を設定: X={point.X}, Y={point.Y}";
                        return;
                    }
                    // 窓2-座標2の登録キー
                    else if (KeyMatches(keyString, e, txtRegKey22.Text))
                    {
                        txtX22.Text = point.X.ToString();
                        txtY22.Text = point.Y.ToString();
                        txtStatus.Text = $"窓2-座標2を設定: X={point.X}, Y={point.Y}";
                        return;
                    }
                    // 窓3-座標1の登録キー
                    else if (KeyMatches(keyString, e, txtRegKey31.Text))
                    {
                        txtX31.Text = point.X.ToString();
                        txtY31.Text = point.Y.ToString();
                        txtStatus.Text = $"窓3-座標1を設定: X={point.X}, Y={point.Y}";
                        return;
                    }
                    // 窓3-座標2の登録キー
                    else if (KeyMatches(keyString, e, txtRegKey32.Text))
                    {
                        txtX32.Text = point.X.ToString();
                        txtY32.Text = point.Y.ToString();
                        txtStatus.Text = $"窓3-座標2を設定: X={point.X}, Y={point.Y}";
                        return;
                    }
                }
            });
        }

        private bool KeyMatches(string keyString, Key e, string targetKey)
        {
            string target = targetKey.ToUpper();
            if (keyString == target)
                return true;
            if (e >= Key.D1 && e <= Key.D9 && keyString == "D" + target)
                return true;
            return false;
        }

        private void PerformJudgement()
        {
            if (overlayWindow == null || !overlayWindow.IsVisible)
            {
                txtStatus.Text = "エラー: 表示ウィンドウが起動していません";
                return;
            }

            try
            {
                // フォントサイズを適用
                if (int.TryParse(txtFontSize.Text, out int fontSize) && fontSize > 0)
                {
                    overlayWindow.SetFontSize(fontSize);
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
                overlayWindow.UpdateJudgementResult(1, 1, $"{lowest11} ({result1})", color11);
                overlayWindow.UpdateJudgementResult(1, 2, $"{lowest12} ({result1})", color12);
                overlayWindow.UpdateJudgementResult(2, 1, $"{lowest21} ({result2})", color21);
                overlayWindow.UpdateJudgementResult(2, 2, $"{lowest22} ({result2})", color22);
                overlayWindow.UpdateJudgementResult(3, 1, $"{lowest31} ({result3})", color31);
                overlayWindow.UpdateJudgementResult(3, 2, $"{lowest32} ({result3})", color32);

                // メイン表示を更新（最初にマッチしたルールを表示）
                if (!string.IsNullOrEmpty(result1))
                {
                    overlayWindow.ShowResultText(result1);
                }
                else if (!string.IsNullOrEmpty(result2))
                {
                    overlayWindow.ShowResultText(result2);
                }
                else if (!string.IsNullOrEmpty(result3))
                {
                    overlayWindow.ShowResultText(result3);
                }

                // 週を自動的に更新（1-16でループ）
                currentWeek++;
                if (currentWeek > 16)
                {
                    currentWeek = 1;
                }
                txtWeek.Text = currentWeek.ToString();
                overlayWindow.SetWeekInfo(currentWeek);

                txtStatus.Text = $"判別完了 - {currentWeek}週目";
            }
            catch (Exception ex)
            {
                txtStatus.Text = $"エラー: {ex.Message}";
            }
        }

        private string GetLowestRGB(Color color)
        {
            if (color.R < color.G && color.R < color.B)
                return "R";
            else if (color.G < color.R && color.G < color.B)
                return "G";
            else if (color.B < color.R && color.B < color.G)
                return "B";
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
                ruleSet.Rule1_Coord1RGB = GetComboBoxValue($"cmb{tabPrefix}Rule1Coord1RGB", "R");
                ruleSet.Rule1_Coord2RGB = GetComboBoxValue($"cmb{tabPrefix}Rule1Coord2RGB", "G");
                ruleSet.Rule1_Display1 = GetComboBoxValue($"cmb{tabPrefix}Rule1Display1", "Vo");
                ruleSet.Rule1_Display2 = GetComboBoxValue($"cmb{tabPrefix}Rule1Display2", "Vo");

                ruleSet.Rule2_Coord1RGB = GetComboBoxValue($"cmb{tabPrefix}Rule2Coord1RGB", "G");
                ruleSet.Rule2_Coord2RGB = GetComboBoxValue($"cmb{tabPrefix}Rule2Coord2RGB", "R");
                ruleSet.Rule2_Display1 = GetComboBoxValue($"cmb{tabPrefix}Rule2Display1", "Da");
                ruleSet.Rule2_Display2 = GetComboBoxValue($"cmb{tabPrefix}Rule2Display2", "Da");

                ruleSet.Rule3_Coord1RGB = GetComboBoxValue($"cmb{tabPrefix}Rule3Coord1RGB", "B");
                ruleSet.Rule3_Coord2RGB = GetComboBoxValue($"cmb{tabPrefix}Rule3Coord2RGB", "B");
                ruleSet.Rule3_Display1 = GetComboBoxValue($"cmb{tabPrefix}Rule3Display1", "Vi");
                ruleSet.Rule3_Display2 = GetComboBoxValue($"cmb{tabPrefix}Rule3Display2", "Vi");

                ruleSet.Rule4_Coord1RGB = GetComboBoxValue($"cmb{tabPrefix}Rule4Coord1RGB", "R");
                ruleSet.Rule4_Coord2RGB = GetComboBoxValue($"cmb{tabPrefix}Rule4Coord2RGB", "B");
                ruleSet.Rule4_Display1 = GetComboBoxValue($"cmb{tabPrefix}Rule4Display1", "Vi");
                ruleSet.Rule4_Display2 = GetComboBoxValue($"cmb{tabPrefix}Rule4Display2", "Da");

                ruleSet.Rule5_Coord1RGB = GetComboBoxValue($"cmb{tabPrefix}Rule5Coord1RGB", "B");
                ruleSet.Rule5_Coord2RGB = GetComboBoxValue($"cmb{tabPrefix}Rule5Coord2RGB", "R");
                ruleSet.Rule5_Display1 = GetComboBoxValue($"cmb{tabPrefix}Rule5Display1", "Da");
                ruleSet.Rule5_Display2 = GetComboBoxValue($"cmb{tabPrefix}Rule5Display2", "Vi");

                ruleSet.Rule6_Coord1RGB = GetComboBoxValue($"cmb{tabPrefix}Rule6Coord1RGB", "G");
                ruleSet.Rule6_Coord2RGB = GetComboBoxValue($"cmb{tabPrefix}Rule6Coord2RGB", "B");
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
                "Vo" => txtVoDisplayName.Text,
                "Da" => txtDaDisplayName.Text,
                "Vi" => txtViDisplayName.Text,
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
            if (overlayWindow == null || !overlayWindow.IsVisible)
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
                overlayWindow.HideAll();


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
            // 表示ウィンドウを起動時に表示
            if (overlayWindow == null || !overlayWindow.IsVisible)
            {
                overlayWindow = new OverlayWindow();
                overlayWindow.Closed += OverlayWindow_Closed;
                overlayWindow.Show();
                
                // 現在の週情報とフォントサイズを設定
                overlayWindow.SetWeekInfo(currentWeek);
                if (int.TryParse(txtFontSize.Text, out int fontSize) && fontSize > 0)
                {
                    overlayWindow.SetFontSize(fontSize);
                }
            }
            
            // 常時監視タイマーは起動しない（判別キーを押したときのみ識別）
            // detectionTimer.Start();
            
            btnLaunch.IsEnabled = false;
            txtStatus.Text = "表示ウィンドウを起動しました（判別キーで識別実行）";
        }

        private void OverlayWindow_Closed(object? sender, EventArgs e)
        {
            // ウィンドウが閉じられたときの処理
            if (overlayWindow != null)
            {
                overlayWindow.Closed -= OverlayWindow_Closed;
                overlayWindow = null;
            }
            
            btnLaunch.IsEnabled = true;
            txtStatus.Text = "表示ウィンドウが閉じられました（再度開く場合は表示ボタンをクリック）";
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            detectionTimer.Stop();
            mouseTrackingTimer.Stop();
            keyboardHook?.Stop();
            keyboardHook?.Dispose();
            
            // オーバーレイウィンドウを閉じる
            if (overlayWindow != null)
            {
                overlayWindow.Close();
            }
            
            this.Close();
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
                    Window1_Key1 = txtRegKey11.Text,
                    Window1_Key2 = txtRegKey12.Text,

                    Window2_X1 = int.Parse(txtX21.Text),
                    Window2_Y1 = int.Parse(txtY21.Text),
                    Window2_X2 = int.Parse(txtX22.Text),
                    Window2_Y2 = int.Parse(txtY22.Text),
                    Window2_Key1 = txtRegKey21.Text,
                    Window2_Key2 = txtRegKey22.Text,

                    Window3_X1 = int.Parse(txtX31.Text),
                    Window3_Y1 = int.Parse(txtY31.Text),
                    Window3_X2 = int.Parse(txtX32.Text),
                    Window3_Y2 = int.Parse(txtY32.Text),
                    Window3_Key1 = txtRegKey31.Text,
                    Window3_Key2 = txtRegKey32.Text,

                    JudgeKey = txtJudgeKey.Text,
                    CurrentWeek = currentWeek,
                    FontSize = int.TryParse(txtFontSize.Text, out int fontSize) ? fontSize : 200,

                    VoDisplayName = txtVoDisplayName.Text,
                    DaDisplayName = txtDaDisplayName.Text,
                    ViDisplayName = txtViDisplayName.Text
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
                txtRegKey11.Text = settings.Window1_Key1;
                txtRegKey12.Text = settings.Window1_Key2;

                txtX21.Text = settings.Window2_X1.ToString();
                txtY21.Text = settings.Window2_Y1.ToString();
                txtX22.Text = settings.Window2_X2.ToString();
                txtY22.Text = settings.Window2_Y2.ToString();
                txtRegKey21.Text = settings.Window2_Key1;
                txtRegKey22.Text = settings.Window2_Key2;

                txtX31.Text = settings.Window3_X1.ToString();
                txtY31.Text = settings.Window3_Y1.ToString();
                txtX32.Text = settings.Window3_X2.ToString();
                txtY32.Text = settings.Window3_Y2.ToString();
                txtRegKey31.Text = settings.Window3_Key1;
                txtRegKey32.Text = settings.Window3_Key2;

                txtJudgeKey.Text = settings.JudgeKey;
                currentWeek = settings.CurrentWeek;
                txtWeek.Text = currentWeek.ToString();
                txtFontSize.Text = settings.FontSize.ToString();


                // 表示名を復元
                txtVoDisplayName.Text = settings.VoDisplayName;
                txtDaDisplayName.Text = settings.DaDisplayName;
                txtViDisplayName.Text = settings.ViDisplayName;
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
    }
}