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
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace todolist
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<TodoItem> TodoItems { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            TodoItems = new ObservableCollection<TodoItem>();
            TaskListBox.ItemsSource = TodoItems;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            AddTask();
        }

        private void NewTaskTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddTask();
            }
        }

        private void AddTask()
        {
            if (!string.IsNullOrWhiteSpace(NewTaskTextBox.Text))
            {
                var dueDate = DueDatePicker.SelectedDate ?? DateTime.Today;
                TodoItems.Add(new TodoItem 
                { 
                    TaskName = NewTaskTextBox.Text,
                    DueDate = dueDate
                });
                NewTaskTextBox.Clear();
                NewTaskTextBox.Focus();
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is TodoItem item)
            {
                TodoItems.Remove(item);
            }
        }

        private void EditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is TodoItem item)
            {
                var dialog = new Window
                {
                    Title = "Edit Task",
                    Height = 200,
                    Width = 400,
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEADBCC")),
                    ResizeMode = ResizeMode.NoResize,
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                var grid = new Grid { Margin = new Thickness(20) };
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var label1 = new TextBlock { Text = "Task Name:", Foreground = Brushes.Black, Margin = new Thickness(0, 0, 0, 5) };
                Grid.SetRow(label1, 0);
                grid.Children.Add(label1);

                var taskNameTextBox = new TextBox 
                { 
                    Height = 30, 
                    VerticalContentAlignment = VerticalAlignment.Center, 
                    Foreground = Brushes.Black,
                    Text = item.TaskName
                };
                Grid.SetRow(taskNameTextBox, 1);
                grid.Children.Add(taskNameTextBox);

                var datePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 15, 0, 0) };
                var label2 = new TextBlock { Text = "Due Date:", Foreground = Brushes.Black, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) };
                var datePicker = new DatePicker { Width = 150, Height = 30, SelectedDate = item.DueDate };
                datePanel.Children.Add(label2);
                datePanel.Children.Add(datePicker);
                Grid.SetRow(datePanel, 2);
                grid.Children.Add(datePanel);

                var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 20, 0, 0) };
                var okButton = new Button 
                { 
                    Content = "OK", 
                    Width = 80, 
                    Height = 30, 
                    Background = Brushes.White, 
                    Foreground = Brushes.Black 
                };
                okButton.Click += (s, args) =>
                {
                    if (!string.IsNullOrWhiteSpace(taskNameTextBox.Text))
                    {
                        item.TaskName = taskNameTextBox.Text;
                        item.DueDate = datePicker.SelectedDate ?? DateTime.Today;
                        dialog.DialogResult = true;
                        dialog.Close();
                    }
                    else
                    {
                        MessageBox.Show("Please enter a task name.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                };

                var cancelButton = new Button 
                { 
                    Content = "Cancel", 
                    Width = 80, 
                    Height = 30, 
                    Margin = new Thickness(10, 0, 0, 0), 
                    Background = Brushes.White, 
                    Foreground = Brushes.Black 
                };
                cancelButton.Click += (s, args) =>
                {
                    dialog.DialogResult = false;
                    dialog.Close();
                };

                buttonPanel.Children.Add(okButton);
                buttonPanel.Children.Add(cancelButton);
                Grid.SetRow(buttonPanel, 3);
                grid.Children.Add(buttonPanel);

                dialog.Content = grid;
                taskNameTextBox.Focus();
                taskNameTextBox.SelectAll();

                dialog.ShowDialog();
            }
        }
    }

    public class TodoItem : INotifyPropertyChanged
    {
        private string taskName;
        private bool isCompleted;
        private DateTime dueDate;

        public string TaskName
        {
            get => taskName;
            set
            {
                taskName = value;
                OnPropertyChanged(nameof(TaskName));
            }
        }

        public bool IsCompleted
        {
            get => isCompleted;
            set
            {
                isCompleted = value;
                OnPropertyChanged(nameof(IsCompleted));
            }
        }

        public DateTime DueDate
        {
            get => dueDate;
            set
            {
                dueDate = value;
                OnPropertyChanged(nameof(DueDate));
                OnPropertyChanged(nameof(DueDateDisplay));
            }
        }

        public string DueDateDisplay => DueDate.ToString("yyyy/MM/dd");

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}