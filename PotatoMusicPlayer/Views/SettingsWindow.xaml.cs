using System;
using System.Windows;
using PotatoMusicPlayer.Services;
using PotatoMusicPlayer.Models;
using Newtonsoft.Json;

namespace PotatoMusicPlayer.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly SettingsService _settingsService;
        private readonly LanguageService _languageService;
        private AppSettings _editableSettings;
        private HotKeyBinding _capturingBinding;

        public SettingsWindow(SettingsService settingsService)
        {
            InitializeComponent();
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

            // Deep copy settings to editable instance
            var original = _settingsService.GetSettings();
            var json = JsonConvert.SerializeObject(original);
            _editableSettings = JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
            _languageService = new LanguageService(_editableSettings.Language);
            ApplyLanguage();
            HotKeyItemsControl.ItemsSource = _editableSettings.HotKeyBindings;
            SkipDurationText.Text = _editableSettings.SkipDurationSeconds.ToString();
            StepDurationText.Text = _editableSettings.StepDurationSeconds.ToString("0.##");
            SpeedStepText.Text = _editableSettings.SpeedChangePercent.ToString();
            ScrollVolumeText.Text = _editableSettings.ScrollVolumeChangePercent.ToString();
            HotkeyVolumeText.Text = _editableSettings.VolumeChangePercent.ToString();
            SpeedResetText.Text = _editableSettings.SpeedResetPercent.ToString();
            UpdateHotKeyDisplayNames();
            PreviewKeyDown += SettingsWindow_PreviewKeyDown;

            // Initialize UI controls
            MaxVolumeSlider.Value = _editableSettings.MaxVolumeMultiplier * 100.0;
            MaxVolumeText.Text = ((int)(MaxVolumeSlider.Value)).ToString();

            MaxVolumeSlider.ValueChanged += (s, e) =>
            {
                MaxVolumeText.Text = ((int)MaxVolumeSlider.Value).ToString();
            };

            MaxVolumeText.LostFocus += (s, e) =>
            {
                if (int.TryParse(MaxVolumeText.Text, out var v))
                {
                    v = Math.Clamp(v, 100, 400);
                    MaxVolumeSlider.Value = v;
                    MaxVolumeText.Text = v.ToString();
                }
                else
                {
                    MaxVolumeText.Text = ((int)MaxVolumeSlider.Value).ToString();
                }
            };

            // ShowWaveform
            ShowWaveformCheck.IsChecked = _editableSettings.ShowWaveform;

            LanguageComboBox.SelectedIndex = _editableSettings.Language == PotatoMusicPlayer.Models.Language.Japanese ? 0 : 1;
            ThemeComboBox.SelectedIndex = (int)_editableSettings.Theme;

            // default selection
            CategoryList.SelectedIndex = 0; // select "一般" by default
        }

        private bool ApplyCurrentSettings()
        {
            // Apply edited values to the original settings instance and save
            _editableSettings.MaxVolumeMultiplier = (float)(MaxVolumeSlider.Value / 100.0);
            _editableSettings.ShowWaveform = ShowWaveformCheck.IsChecked == true;
            if (!TryReadNumericSettings() || HasDuplicateHotKeys())
                return false;
            if (LanguageComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem languageItem &&
                Enum.TryParse(languageItem.Tag?.ToString(), out PotatoMusicPlayer.Models.Language language))
            {
                _editableSettings.Language = language;
            }
            if (ThemeComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem themeItem &&
                Enum.TryParse(themeItem.Tag?.ToString(), out ThemeMode theme))
            {
                _editableSettings.Theme = theme;
            }
            _settingsService.SaveSettings(_editableSettings);
            _languageService.Load(_editableSettings.Language);
            ThemeService.Apply(_editableSettings.Theme);
            ApplyLanguage();
            return true;
        }

        private bool TryReadNumericSettings()
        {
            if (!int.TryParse(SkipDurationText.Text, out int skip) || skip < 1 || skip > 3600 ||
                !float.TryParse(StepDurationText.Text, out float step) || step <= 0 || step > 60 ||
                !int.TryParse(ScrollVolumeText.Text, out int scrollVolume) || scrollVolume < 1 || scrollVolume > 100 ||
                !int.TryParse(HotkeyVolumeText.Text, out int volumeStep) || volumeStep < 1 || volumeStep > 100 ||
                !int.TryParse(SpeedStepText.Text, out int speedStep) || speedStep < 1 || speedStep > 100 ||
                !int.TryParse(SpeedResetText.Text, out int speedReset) || speedReset < 1 || speedReset > 400)
            {
                MessageBox.Show("数値設定を確認してください。", "設定", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            _editableSettings.SkipDurationSeconds = skip;
            _editableSettings.StepDurationSeconds = step;
            _editableSettings.ScrollVolumeChangePercent = scrollVolume;
            _editableSettings.VolumeChangePercent = volumeStep;
            _editableSettings.SpeedChangePercent = speedStep;
            _editableSettings.SpeedResetPercent = speedReset;
            UpdateHotKeyDisplayNames();
            return true;
        }

        private bool HasDuplicateHotKeys()
        {
            for (int i = 0; i < _editableSettings.HotKeyBindings.Count; i++)
            {
                for (int j = i + 1; j < _editableSettings.HotKeyBindings.Count; j++)
                {
                    var left = _editableSettings.HotKeyBindings[i];
                    var right = _editableSettings.HotKeyBindings[j];
                    if (left.Key != System.Windows.Input.Key.None && left.Key == right.Key && left.Modifiers == right.Modifiers)
                    {
                        MessageBox.Show($"ホットキーが重複しています: {left}", "設定", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return true;
                    }
                }
            }
            return false;
        }

        private void HotKeyButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.DataContext is HotKeyBinding binding)
            {
                _capturingBinding = binding;
                button.Content = "キーを入力...";
                button.Focus();
            }
        }

        private void ResetHotKeyButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.DataContext is HotKeyBinding binding)
            {
                var defaults = new AppSettings();
                defaults.InitializeDefaultHotKeys();
                var defaultBinding = defaults.HotKeyBindings.Find(item => item.Action == binding.Action);
                if (defaultBinding != null)
                {
                    binding.Key = defaultBinding.Key;
                    binding.Modifiers = defaultBinding.Modifiers;
                }
                UpdateHotKeyDisplayNames();
            }
        }

        private void ClearHotKeyButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.DataContext is HotKeyBinding binding)
            {
                binding.Key = System.Windows.Input.Key.None;
                binding.Modifiers = System.Windows.Input.ModifierKeys.None;
                UpdateHotKeyDisplayNames();
            }
        }

        private void UpdateHotKeyDisplayNames()
        {
            foreach (var binding in _editableSettings.HotKeyBindings)
            {
                switch (binding.Action)
                {
                    case HotKeyAction.SkipBackward5s: binding.DisplayName = $"{_editableSettings.SkipDurationSeconds}秒戻る"; break;
                    case HotKeyAction.SkipForward5s: binding.DisplayName = $"{_editableSettings.SkipDurationSeconds}秒進む"; break;
                    case HotKeyAction.StepBackward01s: binding.DisplayName = $"{_editableSettings.StepDurationSeconds:0.##}秒戻る"; break;
                    case HotKeyAction.StepForward01s: binding.DisplayName = $"{_editableSettings.StepDurationSeconds:0.##}秒進む"; break;
                    case HotKeyAction.SpeedDecrease: binding.DisplayName = $"速度低下({_editableSettings.SpeedChangePercent}%)"; break;
                    case HotKeyAction.SpeedIncrease: binding.DisplayName = $"速度上昇({_editableSettings.SpeedChangePercent}%)"; break;
                    case HotKeyAction.SpeedReset: binding.DisplayName = $"速度リセット({_editableSettings.SpeedResetPercent}%)"; break;
                }
            }
            HotKeyItemsControl?.Items.Refresh();
        }

        private void SettingsWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (_capturingBinding == null)
                return;

            if (e.Key == System.Windows.Input.Key.Escape)
            {
                _capturingBinding = null;
                HotKeyItemsControl.Items.Refresh();
                e.Handled = true;
                return;
            }

            if (e.Key == System.Windows.Input.Key.LeftCtrl ||
                e.Key == System.Windows.Input.Key.RightCtrl || e.Key == System.Windows.Input.Key.LeftAlt ||
                e.Key == System.Windows.Input.Key.RightAlt || e.Key == System.Windows.Input.Key.LeftShift ||
                e.Key == System.Windows.Input.Key.RightShift)
                return;

            _capturingBinding.Key = e.Key == System.Windows.Input.Key.System ? e.SystemKey : e.Key;
            _capturingBinding.Modifiers = System.Windows.Input.Keyboard.Modifiers;
            _capturingBinding.DisplayName = _capturingBinding.DisplayName ?? _capturingBinding.Action.ToString();
            _capturingBinding = null;
            HotKeyItemsControl.Items.Refresh();
            e.Handled = true;
        }

        private void ApplyLanguage()
        {
            Title = _languageService.Get("Settings.Title");
            GeneralCategoryItem.Content = _languageService.Get("Settings.General");
            VolumeCategoryItem.Content = _languageService.Get("Settings.Volume");
            DisplayCategoryItem.Content = _languageService.Get("Settings.Display");
            HotkeysCategoryItem.Content = _languageService.Get("Settings.Hotkeys");
            NumericCategoryItem.Content = _languageService.Get("Settings.Numeric");
            GeneralTitleText.Text = _languageService.Get("Settings.GeneralTitle");
            LanguageLabelText.Text = _languageService.Get("Settings.Language");
            ThemeLabelText.Text = _languageService.Get("Settings.Theme");
            LightThemeItem.Content = _languageService.Get("Theme.Light");
            DarkThemeItem.Content = _languageService.Get("Theme.Dark");
            SystemThemeItem.Content = _languageService.Get("Theme.System");
            VolumeTitleText.Text = _languageService.Get("Settings.VolumeTitle");
            MaxVolumeLabelText.Text = _languageService.Get("Settings.MaxVolume");
            VolumeExampleText.Text = _languageService.Get("Settings.VolumeExample");
            DisplayTitleText.Text = _languageService.Get("Settings.DisplayTitle");
            ShowWaveformLabelText.Text = _languageService.Get("Settings.ShowWaveform");
            HotkeysTitleText.Text = _languageService.Get("Settings.HotkeysTitle");
            HotkeysHintText.Text = _languageService.Get("Settings.HotkeysHint");
            SkipDurationLabelText.Text = _languageService.Get("Settings.SkipDuration");
            SmallSkipDurationLabelText.Text = _languageService.Get("Settings.StepDuration");
            ScrollVolumeLabelText.Text = _languageService.Get("Settings.ScrollVolume");
            HotkeyVolumeLabelText.Text = _languageService.Get("Settings.VolumeStep");
            SpeedStepLabelText.Text = _languageService.Get("Settings.SpeedStep");
            SpeedResetLabelText.Text = _languageService.Get("Settings.SpeedReset");
            NumericTitleText.Text = _languageService.Get("Settings.NumericTitle");
            JapaneseLanguageItem.Content = _languageService.Get("Language.Japanese");
            EnglishLanguageItem.Content = _languageService.Get("Language.EnglishUS");
            CancelButton.Content = _languageService.Get("Common.Cancel");
            ApplyButton.Content = _languageService.Get("Common.Apply");
            OkButton.Content = _languageService.Get("Common.OK");
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Discard changes
            DialogResult = false;
            Close();
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyCurrentSettings();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (ApplyCurrentSettings())
            {
                DialogResult = true;
                Close();
            }
        }

        private void CategoryList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var item = CategoryList.SelectedItem as System.Windows.Controls.ListBoxItem;
            if (item == null) return;
            var tag = item.Tag as string ?? "";
            GeneralPanel.Visibility = tag == "General" ? Visibility.Visible : Visibility.Collapsed;
            HotkeysPanel.Visibility = tag == "Hotkeys" ? Visibility.Visible : Visibility.Collapsed;
            NumericPanel.Visibility = tag == "Numeric" ? Visibility.Visible : Visibility.Collapsed;
            VolumePanel.Visibility = tag == "Volume" ? Visibility.Visible : Visibility.Collapsed;
            DisplayPanel.Visibility = tag == "Display" ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
