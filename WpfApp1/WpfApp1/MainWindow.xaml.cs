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

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer timer;
        private TimeSpan elapsedTime;
        private bool isRunning;

        public MainWindow()
        {
            InitializeComponent();
            
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += Timer_Tick;
            
            elapsedTime = TimeSpan.Zero;
            isRunning = false;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            elapsedTime = elapsedTime.Add(TimeSpan.FromMilliseconds(100));
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            TimerDisplay.Text = string.Format("{0:D2}:{1:D2}:{2:D2}.{3:D1}",
                (int)elapsedTime.TotalHours,
                elapsedTime.Minutes,
                elapsedTime.Seconds,
                elapsedTime.Milliseconds / 100);
        }

        private void StartStopButton_Click(object sender, RoutedEventArgs e)
        {
            if (isRunning)
            {
                timer.Stop();
                StartStopButton.Content = "スタート";
                isRunning = false;
            }
            else
            {
                timer.Start();
                StartStopButton.Content = "ストップ";
                isRunning = true;
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            timer.Stop();
            elapsedTime = TimeSpan.Zero;
            UpdateDisplay();
            StartStopButton.Content = "スタート";
            isRunning = false;
        }
    }
}