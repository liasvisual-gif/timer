using System.Diagnostics;
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

namespace PCupTimer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer timer;
        private Stopwatch stopwatch;
        private bool isRunning;

        public MainWindow()
        {
            InitializeComponent();
            
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += Timer_Tick;
            
            stopwatch = new Stopwatch();
            isRunning = false;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            double totalSeconds = stopwatch.Elapsed.TotalSeconds;
            
            if (totalSeconds >= 100.0)
            {
                stopwatch.Restart();
                totalSeconds = 0;
            }
            
            if (totalSeconds >= 30.0 && MainGrid.Background == Brushes.Yellow)
            {
                MainGrid.Background = Brushes.Blue;
            }
            
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            double totalSeconds = stopwatch.Elapsed.TotalSeconds;
            int seconds = (int)totalSeconds;
            int tenths = (int)((totalSeconds - seconds) * 10);
            
            TimerDisplay.Text = string.Format("{0:D2}.{1:D1}", seconds, tenths);
        }

        private void StartStopButton_Click(object sender, RoutedEventArgs e)
        {
            if (isRunning)
            {
                timer.Stop();
                stopwatch.Stop();
                StartStopButton.Content = "スタート";
                isRunning = false;
            }
            else
            {
                timer.Start();
                stopwatch.Start();
                StartStopButton.Content = "ストップ";
                MainGrid.Background = Brushes.Yellow;
                isRunning = true;
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            stopwatch.Reset();
            stopwatch.Start();
            timer.Start();
            UpdateDisplay();
            StartStopButton.Content = "ストップ";
            MainGrid.Background = Brushes.Yellow;
            isRunning = true;
        }
    }
}