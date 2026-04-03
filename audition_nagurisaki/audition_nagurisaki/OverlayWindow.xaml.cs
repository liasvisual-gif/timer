using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace audition_nagurisaki
{
    public partial class OverlayWindow : Window
    {
        private bool isTransparentMode = true;

        public OverlayWindow()
        {
            InitializeComponent();
            this.Title = "Naguri Saki";
            SetTransparentMode(true);
        }

        private void ContextMenu_Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ContextMenu_ToggleTransparent_Click(object sender, RoutedEventArgs e)
        {
            isTransparentMode = !isTransparentMode;
            SetTransparentMode(isTransparentMode);
        }

        private void SetTransparentMode(bool transparent)
        {
            isTransparentMode = transparent;
            
            if (transparent)
            {
                bgPanel.Visibility = Visibility.Collapsed;
                this.Width = 400;
                this.Height = 300;
            }
            else
            {
                bgPanel.Visibility = Visibility.Visible;
                this.Width = 500;
                this.Height = 400;
            }
        }

        private HashSet<int> _bomberWeeks = new() { 8, 13 };

        public void SetWeekInfo(int week)
        {
            string weekText = ConvertWeekToText(week);
            txtWeekInfo.Text = weekText;

            // 3-3と4-6のテキストを赤色にする
            if (weekText == "3-3" || weekText == "4-6")
            {
                txtWeekInfo.Fill = new SolidColorBrush(Colors.Red);
            }
            else
            {
                txtWeekInfo.Fill = new SolidColorBrush(Colors.White);
            }

            // ボマー表示
            if (_bomberWeeks.Contains(week))
            {
                txtBomber.Visibility = Visibility.Visible;
            }
            else
            {
                txtBomber.Visibility = Visibility.Collapsed;
            }
        }

        public void SetBomberWeeks(HashSet<int> weeks)
        {
            _bomberWeeks = weeks;
        }

        private string ConvertWeekToText(int week) => WeekHelper.ToLabel(week);

        public void UpdateJudgementResult(int window, int coord, string result, Color color)
        {
            // No display needed for now
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        public void ShowVo(bool show)
        {
            if (isTransparentMode)
            {
                txtVo.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                txtVoWithBg.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public void ShowDa(bool show)
        {
            if (isTransparentMode)
            {
                txtDa.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                txtDaWithBg.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public void ShowVi(bool show)
        {
            if (isTransparentMode)
            {
                txtVi.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                txtViWithBg.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public void HideAll()
        {
            txtVo.Visibility = Visibility.Collapsed;
            txtDa.Visibility = Visibility.Collapsed;
            txtVi.Visibility = Visibility.Collapsed;
            txtVoWithBg.Visibility = Visibility.Collapsed;
            txtDaWithBg.Visibility = Visibility.Collapsed;
            txtViWithBg.Visibility = Visibility.Collapsed;
            coloredResultPanel.Visibility = Visibility.Collapsed;
        }

        public void ShowResultText(string result)
        {
            HideAll();

            // "Vo→Da"のような形式を解析して色付き表示
            if (result.Contains("→"))
            {
                var parts = result.Split('→');
                if (parts.Length >= 2)
                {
                    string part1 = parts[0].Trim();
                    string part2 = parts[1].Trim();
                    
                    txtResult1.Text = part1;
                    txtResult1.Fill = GetColorForType(part1);
                    
                    txtResult2.Text = part2;
                    txtResult2.Fill = GetColorForType(part2);
                    
                    coloredResultPanel.Visibility = Visibility.Visible;
                    return;
                }
            }

            // 従来の単一表示（フォールバック）
            if (isTransparentMode)
            {
                if (result.Contains("Vo"))
                {
                    txtVo.Text = result;
                    txtVo.Visibility = Visibility.Visible;
                }
                else if (result.Contains("Da"))
                {
                    txtDa.Text = result;
                    txtDa.Visibility = Visibility.Visible;
                }
                else if (result.Contains("Vi"))
                {
                    txtVi.Text = result;
                    txtVi.Visibility = Visibility.Visible;
                }
            }
            else
            {
                if (result.Contains("Vo"))
                {
                    txtVoWithBg.Text = result;
                    txtVoWithBg.Visibility = Visibility.Visible;
                }
                else if (result.Contains("Da"))
                {
                    txtDaWithBg.Text = result;
                    txtDaWithBg.Visibility = Visibility.Visible;
                }
                else if (result.Contains("Vi"))
                {
                    txtViWithBg.Text = result;
                    txtViWithBg.Visibility = Visibility.Visible;
                }
            }
        }
        
        private System.Windows.Media.Brush GetColorForType(string type)
        {
            return type switch
            {
                "Vo" => new SolidColorBrush(Colors.Red),
                "Da" => new SolidColorBrush(Colors.Blue),
                "Vi" => new SolidColorBrush(Colors.Yellow),
                _ => new SolidColorBrush(Colors.White)
            };
        }

        public void SetFontSize(int fontSize)
        {
            txtVo.FontSize = fontSize;
            txtDa.FontSize = fontSize;
            txtVi.FontSize = fontSize;
            txtResult1.FontSize = fontSize;
            txtResultArrow.FontSize = fontSize;
            txtResult2.FontSize = fontSize;

            int bgFontSize = fontSize / 2;
            txtVoWithBg.FontSize = bgFontSize;
            txtDaWithBg.FontSize = bgFontSize;
            txtViWithBg.FontSize = bgFontSize;
        }

        public void SetYamaMode(bool isYamaAri)
        {
            if (isYamaAri)
            {
                txtYamaMode.Text = "山あり";
                txtYamaMode.Fill = new SolidColorBrush(Colors.LimeGreen);
            }
            else
            {
                txtYamaMode.Text = "山なし";
                txtYamaMode.Fill = new SolidColorBrush(Colors.Orange);
            }
        }
        
        /// <summary>
        /// チェックされた属性の殴り先を表示
        /// </summary>
        public void ShowNagurisakiTargets(List<string> targets)
        {
            if (targets == null || targets.Count == 0)
            {
                txtNagurisaki.Text = "";
                return;
            }
            
            // 殴り先をtxtNagurisakiに表示（週情報の横）
            txtNagurisaki.Text = string.Join(" ", targets);
        }
    }
}
