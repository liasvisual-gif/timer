using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace audition_nagurisaki
{
    public partial class PatternEditorWindow : Window
    {
        private ObservableCollection<ClickPreset> _presets;
        private ObservableCollection<ClickPattern> _patterns;

        public PatternEditorWindow(ObservableCollection<ClickPreset> presets, ObservableCollection<ClickPattern> patterns)
        {
            InitializeComponent();
            _presets = presets;
            _patterns = patterns;
            RefreshPatternList();
            RefreshPresetList();
        }

        private void RefreshPatternList()
        {
            int selectedIdx = dgPatterns.SelectedIndex;
            dgPatterns.ItemsSource = null;
            var list = new List<PatternDisplayRow>();
            for (int i = 0; i < _patterns.Count; i++)
            {
                var p = _patterns[i];
                list.Add(new PatternDisplayRow
                {
                    Index = i,
                    Name = p.Name,
                    Hotkey = p.Hotkey,
                    WindowLabel = WindowLabels.GetValueOrDefault(p.WindowNumber, "全窓"),
                    Summary = string.Join(", ", p.PresetNames)
                });
            }
            dgPatterns.ItemsSource = list;
            if (selectedIdx >= 0 && selectedIdx < list.Count)
                dgPatterns.SelectedIndex = selectedIdx;
        }

        private void RefreshPresetList()
        {
            lstAllPresets.Items.Clear();
            for (int i = 0; i < _presets.Count; i++)
                lstAllPresets.Items.Add($"{i}:{_presets[i].Name}");
        }

        private void RefreshSteps()
        {
            dgPatternSteps.ItemsSource = null;
            int patIdx = dgPatterns.SelectedIndex;
            if (patIdx >= 0 && patIdx < _patterns.Count)
            {
                var pattern = _patterns[patIdx];
                var steps = new List<PatternStepView>();
                for (int i = 0; i < pattern.PresetNames.Count; i++)
                {
                    var name = pattern.PresetNames[i];
                    var preset = _presets.FirstOrDefault(p => p.Name == name);
                    steps.Add(new PatternStepView
                    {
                        Idx = i,
                        Name = name,
                        WaitAfter = preset?.Delay ?? 0
                    });
                }
                dgPatternSteps.ItemsSource = steps;
            }
        }

        private void DgPatterns_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int patIdx = dgPatterns.SelectedIndex;
            if (patIdx >= 0 && patIdx < _patterns.Count)
            {
                txtPatternName.Text = _patterns[patIdx].Name;
                txtPatternHotkey.Text = _patterns[patIdx].Hotkey;
                SelectWindowComboBox(_patterns[patIdx].WindowNumber);
                chkAdvanceWeek.IsChecked = _patterns[patIdx].AdvanceWeek;
            }
            else
            {
                txtPatternName.Text = "";
                txtPatternHotkey.Text = "";
                cmbPatternWindow.SelectedIndex = 0;
                chkAdvanceWeek.IsChecked = false;
            }
            RefreshSteps();
        }

        private void BtnAddPattern_Click(object sender, RoutedEventArgs e)
        {
            string name = txtPatternName.Text.Trim();
            if (string.IsNullOrEmpty(name)) name = $"パターン{_patterns.Count + 1}";
            _patterns.Add(new ClickPattern { Name = name, Hotkey = txtPatternHotkey.Text.Trim(), WindowNumber = GetSelectedWindowNumber(), AdvanceWeek = chkAdvanceWeek.IsChecked == true });
            RefreshPatternList();
            dgPatterns.SelectedIndex = dgPatterns.Items.Count - 1;
        }

        private void BtnUpdatePattern_Click(object sender, RoutedEventArgs e)
        {
            int patIdx = dgPatterns.SelectedIndex;
            if (patIdx >= 0 && patIdx < _patterns.Count)
            {
                _patterns[patIdx].Name = txtPatternName.Text;
                _patterns[patIdx].Hotkey = txtPatternHotkey.Text.Trim();
                _patterns[patIdx].WindowNumber = GetSelectedWindowNumber();
                _patterns[patIdx].AdvanceWeek = chkAdvanceWeek.IsChecked == true;
                RefreshPatternList();
            }
        }

        private void BtnDeletePattern_Click(object sender, RoutedEventArgs e)
        {
            int patIdx = dgPatterns.SelectedIndex;
            if (patIdx >= 0 && patIdx < _patterns.Count)
            {
                _patterns.RemoveAt(patIdx);
                RefreshPatternList();
                if (_patterns.Count > 0)
                    dgPatterns.SelectedIndex = Math.Min(patIdx, _patterns.Count - 1);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
        }

        private void BtnAddToPattern_Click(object sender, RoutedEventArgs e)
        {
            int patIdx = dgPatterns.SelectedIndex;
            if (patIdx < 0 || patIdx >= _patterns.Count) return;
            if (lstAllPresets.SelectedIndex < 0 || lstAllPresets.SelectedIndex >= _presets.Count) return;

            var pattern = _patterns[patIdx];
            var preset = _presets[lstAllPresets.SelectedIndex];
            pattern.PresetNames.Add(preset.Name);
            RefreshSteps();
            RefreshPatternList();
        }

        private void BtnRemoveFromPattern_Click(object sender, RoutedEventArgs e)
        {
            int patIdx = dgPatterns.SelectedIndex;
            if (patIdx < 0 || patIdx >= _patterns.Count) return;
            int stepIdx = dgPatternSteps.SelectedIndex;
            if (stepIdx < 0) return;

            var pattern = _patterns[patIdx];
            if (stepIdx < pattern.PresetNames.Count)
            {
                pattern.PresetNames.RemoveAt(stepIdx);
                RefreshSteps();
                RefreshPatternList();
            }
        }

        private void BtnMoveStepUp_Click(object sender, RoutedEventArgs e)
        {
            int patIdx = dgPatterns.SelectedIndex;
            if (patIdx < 0 || patIdx >= _patterns.Count) return;
            var pattern = _patterns[patIdx];
            int idx = dgPatternSteps.SelectedIndex;
            if (idx > 0)
            {
                (pattern.PresetNames[idx], pattern.PresetNames[idx - 1]) = (pattern.PresetNames[idx - 1], pattern.PresetNames[idx]);
                RefreshSteps();
                dgPatternSteps.SelectedIndex = idx - 1;
                RefreshPatternList();
            }
        }

        private void BtnMoveStepDown_Click(object sender, RoutedEventArgs e)
        {
            int patIdx = dgPatterns.SelectedIndex;
            if (patIdx < 0 || patIdx >= _patterns.Count) return;
            var pattern = _patterns[patIdx];
            int idx = dgPatternSteps.SelectedIndex;
            if (idx >= 0 && idx < pattern.PresetNames.Count - 1)
            {
                (pattern.PresetNames[idx], pattern.PresetNames[idx + 1]) = (pattern.PresetNames[idx + 1], pattern.PresetNames[idx]);
                RefreshSteps();
                dgPatternSteps.SelectedIndex = idx + 1;
                RefreshPatternList();
            }
        }

        private static readonly Dictionary<int, string> WindowLabels = new()
        {
            { -1, "全窓" }, { 0, "単窓" }, { 1, "8窓1" }, { 2, "8窓2" }, { 3, "8窓3" },
            { 4, "3窓1" }, { 5, "3窓2" }, { 6, "3窓3" }
        };

        private class PatternDisplayRow
        {
            public int Index { get; set; }
            public string Name { get; set; } = "";
            public string Hotkey { get; set; } = "";
            public string WindowLabel { get; set; } = "全窓";
            public string Summary { get; set; } = "";
        }

        private int GetSelectedWindowNumber()
        {
            if (cmbPatternWindow.SelectedItem is ComboBoxItem item && int.TryParse(item.Tag?.ToString(), out int wn))
                return wn;
            return -1;
        }

        private void SelectWindowComboBox(int windowNumber)
        {
            for (int i = 0; i < cmbPatternWindow.Items.Count; i++)
            {
                if (cmbPatternWindow.Items[i] is ComboBoxItem item && item.Tag?.ToString() == windowNumber.ToString())
                {
                    cmbPatternWindow.SelectedIndex = i;
                    return;
                }
            }
            cmbPatternWindow.SelectedIndex = 0;
        }

        private class PatternStepView
        {
            public int Idx { get; set; }
            public string Name { get; set; } = "";
            public int WaitAfter { get; set; }
        }
    }
}
