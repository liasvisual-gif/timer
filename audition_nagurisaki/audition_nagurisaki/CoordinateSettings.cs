using System.Text.Json;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;

namespace audition_nagurisaki
{
    // 定点クリックプリセット
    public class ClickPreset : INotifyPropertyChanged
    {
        private string _name = "";
        private int _x;
        private int _y;
        private int _delay;

        public string Name
        {
            get => _name;
            set { _name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name))); }
        }
        public int X
        {
            get => _x;
            set { _x = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(X))); }
        }
        public int Y
        {
            get => _y;
            set { _y = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Y))); }
        }
        public int Delay
        {
            get => _delay;
            set { _delay = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Delay))); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    // クリックパターン（定点の実行順序）
    public class ClickPattern : INotifyPropertyChanged
    {
        private string _name = "";
        private string _hotkey = "";
        private int _windowNumber = -1;
        private bool _advanceWeek = false;
        public string Name
        {
            get => _name;
            set { _name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name))); }
        }
        public string Hotkey
        {
            get => _hotkey;
            set { _hotkey = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Hotkey))); }
        }
        /// <summary>
        /// 対象窓番号。-1=全窓共通, 0=単窓, 1=8窓1, 2=8窓2, 3=8窓3, 4=3窓1, 5=3窓2, 6=3窓3
        /// </summary>
        public int WindowNumber
        {
            get => _windowNumber;
            set { _windowNumber = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WindowNumber))); }
        }
        /// <summary>
        /// パターン実行後に週を進める
        /// </summary>
        public bool AdvanceWeek
        {
            get => _advanceWeek;
            set { _advanceWeek = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AdvanceWeek))); }
        }
        public List<string> PresetNames { get; set; } = new();
        public event PropertyChangedEventHandler? PropertyChanged;
    }

    // 窓別オーディション進行設定
    public class WindowProgressionSettings
    {
        public Dictionary<int, string> YamaAriProgression { get; set; } = new();
        public Dictionary<int, string> YamaNashiProgression { get; set; } = new();
        public List<int> BomberWeeks { get; set; } = new() { 8, 13 };
    }

    // 週番号ラベル変換ヘルパー
    public static class WeekHelper
    {
        private static readonly string[] WeekLabels =
        [
            "4-1", "3-8", "3-7", "3-6", "3-5", "3-4", "3-3", "3-2",
            "3-1", "4-8", "4-7", "4-6", "4-5", "4-4", "4-3", "4-2"
        ];

        public static string ToLabel(int week)
        {
            if (week >= 1 && week <= 16)
                return WeekLabels[week - 1];
            return "4-1";
        }
    }

    // 各タブのルールセット
    public class RuleSet
    {
        public string Rule1_Coord1RGB { get; set; } = "R";
        public string Rule1_Coord2RGB { get; set; } = "G";
        public string Rule1_Display1 { get; set; } = "Vo";
        public string Rule1_Display2 { get; set; } = "Vo";

        public string Rule2_Coord1RGB { get; set; } = "G";
        public string Rule2_Coord2RGB { get; set; } = "R";
        public string Rule2_Display1 { get; set; } = "Da";
        public string Rule2_Display2 { get; set; } = "Da";

        public string Rule3_Coord1RGB { get; set; } = "B";
        public string Rule3_Coord2RGB { get; set; } = "B";
        public string Rule3_Display1 { get; set; } = "Vi";
        public string Rule3_Display2 { get; set; } = "Vi";

        public string Rule4_Coord1RGB { get; set; } = "R";
        public string Rule4_Coord2RGB { get; set; } = "B";
        public string Rule4_Display1 { get; set; } = "Vi";
        public string Rule4_Display2 { get; set; } = "Da";

        public string Rule5_Coord1RGB { get; set; } = "B";
        public string Rule5_Coord2RGB { get; set; } = "R";
        public string Rule5_Display1 { get; set; } = "Da";
        public string Rule5_Display2 { get; set; } = "Vi";

        public string Rule6_Coord1RGB { get; set; } = "G";
        public string Rule6_Coord2RGB { get; set; } = "B";
        public string Rule6_Display1 { get; set; } = "Vo";
        public string Rule6_Display2 { get; set; } = "Vi";
    }

    public class CoordinateSettings
    {
        public int Window1_X1 { get; set; }
        public int Window1_Y1 { get; set; }
        public int Window1_X2 { get; set; }
        public int Window1_Y2 { get; set; }
        public string Window1_Key1 { get; set; } = "1";
        public string Window1_Key2 { get; set; } = "Q";

        public int Window2_X1 { get; set; }
        public int Window2_Y1 { get; set; }
        public int Window2_X2 { get; set; }
        public int Window2_Y2 { get; set; }
        public string Window2_Key1 { get; set; } = "2";
        public string Window2_Key2 { get; set; } = "W";

        public int Window3_X1 { get; set; }
        public int Window3_Y1 { get; set; }
        public int Window3_X2 { get; set; }
        public int Window3_Y2 { get; set; }
        public string Window3_Key1 { get; set; } = "3";
        public string Window3_Key2 { get; set; } = "E";

        // 単窓の座標
        public int WindowSingle_X1 { get; set; }
        public int WindowSingle_Y1 { get; set; }
        public int WindowSingle_X2 { get; set; }
        public int WindowSingle_Y2 { get; set; }

        // 3窓の座標
        public int Window3W1_X1 { get; set; }
        public int Window3W1_Y1 { get; set; }
        public int Window3W1_X2 { get; set; }
        public int Window3W1_Y2 { get; set; }

        public int Window3W2_X1 { get; set; }
        public int Window3W2_Y1 { get; set; }
        public int Window3W2_X2 { get; set; }
        public int Window3W2_Y2 { get; set; }

        public int Window3W3_X1 { get; set; }
        public int Window3W3_Y1 { get; set; }
        public int Window3W3_X2 { get; set; }
        public int Window3W3_Y2 { get; set; }

        public int CurrentWeek { get; set; } = 1;
        public int FontSize { get; set; } = 200;
        
        // 判別キーと窓移動キー
        public string JudgeKey { get; set; } = "Space";
        public string MoveToWindow1Key { get; set; } = "Z";
        public string MoveToWindow2Key { get; set; } = "X";
        public string MoveToWindow3Key { get; set; } = "C";
        public string MoveToWindowSingleKey { get; set; } = "";
        public string MoveToWindow3W1Key { get; set; } = "";
        public string MoveToWindow3W2Key { get; set; } = "";
        public string MoveToWindow3W3Key { get; set; } = "";

        // ルール設定
        public string Rule1_Coord1RGB { get; set; } = "R";
        public string Rule1_Coord2RGB { get; set; } = "G";
        public string Rule1_Display1 { get; set; } = "Vo";
        public string Rule1_Display2 { get; set; } = "Vo";

        public string Rule2_Coord1RGB { get; set; } = "G";
        public string Rule2_Coord2RGB { get; set; } = "R";
        public string Rule2_Display1 { get; set; } = "Da";
        public string Rule2_Display2 { get; set; } = "Da";

        public string Rule3_Coord1RGB { get; set; } = "B";
        public string Rule3_Coord2RGB { get; set; } = "B";
        public string Rule3_Display1 { get; set; } = "Vi";
        public string Rule3_Display2 { get; set; } = "Vi";

        public string Rule4_Coord1RGB { get; set; } = "R";
        public string Rule4_Coord2RGB { get; set; } = "B";
        public string Rule4_Display1 { get; set; } = "Vi";
        public string Rule4_Display2 { get; set; } = "Da";

        public string Rule5_Coord1RGB { get; set; } = "B";
        public string Rule5_Coord2RGB { get; set; } = "R";
        public string Rule5_Display1 { get; set; } = "Da";
        public string Rule5_Display2 { get; set; } = "Vi";

        public string Rule6_Coord1RGB { get; set; } = "G";
        public string Rule6_Coord2RGB { get; set; } = "B";
        public string Rule6_Display1 { get; set; } = "Vo";
        public string Rule6_Display2 { get; set; } = "Vi";

        // 各タブのルール設定を保存するクラス
        public RuleSet DefaultRules { get; set; } = new RuleSet();
        public RuleSet EreeBestRules { get; set; } = new RuleSet();
        public RuleSet SpotlightRules { get; set; } = new RuleSet();
        public RuleSet OdotteRules { get; set; } = new RuleSet();
        public RuleSet LegendRules { get; set; } = new RuleSet();
        public RuleSet NanasaiRules { get; set; } = new RuleSet();
        public RuleSet UtahimeRules { get; set; } = new RuleSet();

        // 定点クリック設定
        public bool AutoClickEnabled { get; set; } = true;
        public string CoordRegKey { get; set; } = "R";

        // 旧互換
        public string FixedClickRegKey { get; set; } = "T";

        // 共有プリセット（動的）
        public List<ClickPreset> ClickPresets { get; set; } = new List<ClickPreset>();

        // クリックパターン
        public List<ClickPattern> ClickPatterns { get; set; } = new();

        // 週別パターン割り当て（週番号 → パターン名）
        public Dictionary<int, string> WeekPatterns { get; set; } = new();

        // 山あり/山なし状態
        public bool IsYamaAri { get; set; } = true;
        public string YamaToggleKey { get; set; } = "";

        // ボマー表示設定（週番号のリスト）- 旧互換
        public List<int> BomberWeeks { get; set; } = new() { 8, 13 };

        // オーディション進行設定（週番号 → オーディションタブ名）- 旧互換
        public Dictionary<int, string> YamaAriProgression { get; set; } = new();
        public Dictionary<int, string> YamaNashiProgression { get; set; } = new();

        // 窓別オーディション進行設定（窓番号 → WindowProgressionSettings）
        public Dictionary<int, WindowProgressionSettings> WindowProgressions { get; set; } = new();

        // 窓別パターン割り当て（旧互換）
        public string Window1PatternName { get; set; } = "";
        public string Window2PatternName { get; set; } = "";
        public string Window3PatternName { get; set; } = "";
        public string WindowSinglePatternName { get; set; } = "";
        public string Window3W1PatternName { get; set; } = "";
        public string Window3W2PatternName { get; set; } = "";
        public string Window3W3PatternName { get; set; } = "";

        // 旧互換（読み込み用）
        public string Window1PresetSequence { get; set; } = "";
        public string Window2PresetSequence { get; set; } = "";
        public string Window3PresetSequence { get; set; } = "";
        public string WindowSinglePresetSequence { get; set; } = "";
        public string Window3W1PresetSequence { get; set; } = "";
        public string Window3W2PresetSequence { get; set; } = "";
        public string Window3W3PresetSequence { get; set; } = "";

        public static CoordinateSettings LoadFromFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    return JsonSerializer.Deserialize<CoordinateSettings>(json, options) ?? new CoordinateSettings();
                }
            }
            catch
            {
                // エラー時はデフォルト設定を返す
            }
            return new CoordinateSettings();
        }

        public void SaveToFile(string filePath)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(filePath, json);
            }
            catch
            {
                // エラーハンドリング
            }
        }
    }
}
