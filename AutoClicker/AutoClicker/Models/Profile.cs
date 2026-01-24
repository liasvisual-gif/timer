using System.Collections.ObjectModel;

namespace AutoClicker.Models
{
    public class Profile
    {
        public string Name { get; set; } = string.Empty;
        public ObservableCollection<ClickPoint> ClickPoints { get; set; } = new ObservableCollection<ClickPoint>();
        public ObservableCollection<OrderClickPoint> OrderPoints { get; set; } = new ObservableCollection<OrderClickPoint>();
    }
}
