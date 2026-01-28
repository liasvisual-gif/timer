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

        public void SetWeekInfo(int week)
        {
            string weekText = ConvertWeekToText(week);
            txtWeekInfo.Text = weekText;
            
            // 3-3と4-6のテキストを赤色にする
            if (weekText == "3-3" || weekText == "4-6")
            {
                txtWeekInfo.Foreground = new SolidColorBrush(Colors.Red);
            }
            else
            {
                txtWeekInfo.Foreground = new SolidColorBrush(Colors.White);
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
        }

        public void ShowResultText(string result)
        {
            HideAll();

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

        public void SetFontSize(int fontSize)
        {
            txtVo.FontSize = fontSize;
            txtDa.FontSize = fontSize;
            txtVi.FontSize = fontSize;
            
            int bgFontSize = fontSize / 2;
            txtVoWithBg.FontSize = bgFontSize;
            txtDaWithBg.FontSize = bgFontSize;
            txtViWithBg.FontSize = bgFontSize;
        }
    }
}
