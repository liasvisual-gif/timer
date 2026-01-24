using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace audition_nagurisaki
{
    public partial class OverlayWindow : Window
    {
        public OverlayWindow()
        {
            InitializeComponent();
            this.Title = "殴り先";
            // 透過ウィンドウなので初期化は最小限
        }

        // 右クリックメニューの閉じるイベント
        private void ContextMenu_Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public void SetWeekInfo(int week)
        {
            // 透過ウィンドウでは週情報を表示しない
        }

        public void UpdateJudgementResult(int window, int coord, string result, Color color)
        {
            // 透過ウィンドウでは個別結果を表示しない
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // ウィンドウをドラッグして移動できるようにする
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        public void ShowVo(bool show)
        {
            txtVo.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        public void ShowDa(bool show)
        {
            txtDa.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        public void ShowVi(bool show)
        {
            txtVi.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        public void HideAll()
        {
            txtVo.Visibility = Visibility.Collapsed;
            txtDa.Visibility = Visibility.Collapsed;
            txtVi.Visibility = Visibility.Collapsed;
        }

        // 矢印形式の結果を表示
        public void ShowResultText(string result)
        {
            // 既存の表示を非表示にして、テキストを更新
            HideAll();

            // 結果に応じて表示
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

        // フォントサイズを設定
        public void SetFontSize(int fontSize)
        {
            txtVo.FontSize = fontSize;
            txtDa.FontSize = fontSize;
            txtVi.FontSize = fontSize;
        }
    }
}
