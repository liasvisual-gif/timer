using System.Text.Json;
using System.IO;

namespace audition_nagurisaki
{
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

        public int CurrentWeek { get; set; } = 1;
        public int FontSize { get; set; } = 200;
        
        // 判別キーと窓移動キー
        public string JudgeKey { get; set; } = "Space";
        public string MoveToWindow1Key { get; set; } = "Z";
        public string MoveToWindow2Key { get; set; } = "X";
        public string MoveToWindow3Key { get; set; } = "C";


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
        public RuleSet SpotlightRules { get; set; } = new RuleSet();
        public RuleSet OdotteRules { get; set; } = new RuleSet();
        public RuleSet LegendRules { get; set; } = new RuleSet();
        public RuleSet NanasaiRules { get; set; } = new RuleSet();
        public RuleSet UtahimeRules { get; set; } = new RuleSet();

        public static CoordinateSettings LoadFromFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    return JsonSerializer.Deserialize<CoordinateSettings>(json) ?? new CoordinateSettings();
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
