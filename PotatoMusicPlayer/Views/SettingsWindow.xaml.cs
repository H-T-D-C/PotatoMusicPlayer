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
            MaxRecentFilesText.Text = _editableSettings.MaxRecentFiles.ToString();
            DefaultVolumeText.Text = ((int)Math.Round(_editableSettings.DefaultVolume * 100)).ToString();
            MaxVolumeText.Text = ((int)Math.Round(_editableSettings.MaxVolumeMultiplier * 100)).ToString();
            UpdateHotKeyDisplayNames();
            PreviewKeyDown += SettingsWindow_PreviewKeyDown;

            // ShowWaveform
            ShowWaveformCheck.IsChecked = _editableSettings.ShowWaveform;

            LanguageComboBox.SelectedIndex = _editableSettings.Language == PotatoMusicPlayer.Models.Language.Japanese ? 0 : 1;
            ThemeComboBox.SelectedIndex = (int)_editableSettings.Theme;
            RememberLastVolumeCheck.IsChecked = _editableSettings.RememberLastVolume;
            RememberLastSpeedCheck.IsChecked = _editableSettings.RememberLastPlaybackSpeed;
            RememberLastLoopCheck.IsChecked = _editableSettings.RememberLastLoopMode;

            // default selection
            CategoryList.SelectedIndex = 0; // select "一般" by default
        }

        private bool ApplyCurrentSettings()
        {
            // Apply edited values to the original settings instance and save
            _editableSettings.ShowWaveform = ShowWaveformCheck.IsChecked == true;
            _editableSettings.RememberLastVolume = RememberLastVolumeCheck.IsChecked == true;
            _editableSettings.RememberLastPlaybackSpeed = RememberLastSpeedCheck.IsChecked == true;
            _editableSettings.RememberLastLoopMode = RememberLastLoopCheck.IsChecked == true;
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
                !int.TryParse(SpeedResetText.Text, out int speedReset) || speedReset < 1 || speedReset > 1000 ||
                !int.TryParse(MaxRecentFilesText.Text, out int maxRecentFiles) || maxRecentFiles < 1 || maxRecentFiles > 1000 ||
                !int.TryParse(DefaultVolumeText.Text, out int defaultVolume) || defaultVolume < 0 || defaultVolume > 1000 ||
                !int.TryParse(MaxVolumeText.Text, out int maxVolume) || maxVolume < 100 || maxVolume > 1000 ||
                defaultVolume > maxVolume)
            {
                MessageBox.Show("数値設定を確認してください。\n入力された値が範囲外であるか、形式が正しくありません。", "設定", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            _editableSettings.SkipDurationSeconds = skip;
            _editableSettings.StepDurationSeconds = step;
            _editableSettings.ScrollVolumeChangePercent = scrollVolume;
            _editableSettings.VolumeChangePercent = volumeStep;
            _editableSettings.SpeedChangePercent = speedStep;
            _editableSettings.SpeedResetPercent = speedReset;
            _editableSettings.MaxRecentFiles = maxRecentFiles;
            _editableSettings.DefaultVolume = defaultVolume / 100.0f;
            _editableSettings.MaxVolumeMultiplier = maxVolume / 100.0f;
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
                    case HotKeyAction.PlayPause: binding.DisplayName = _languageService.Get("Hotkey.PlayPause"); break;
                    case HotKeyAction.Stop: binding.DisplayName = _languageService.Get("Hotkey.Stop"); break;
                    case HotKeyAction.SkipBackward5s: binding.DisplayName = string.Format(_languageService.Get("Hotkey.SkipBackward"), _editableSettings.SkipDurationSeconds); break;
                    case HotKeyAction.SkipForward5s: binding.DisplayName = string.Format(_languageService.Get("Hotkey.SkipForward"), _editableSettings.SkipDurationSeconds); break;
                    case HotKeyAction.VolumeUp: binding.DisplayName = _languageService.Get("Hotkey.VolumeUp"); break;
                    case HotKeyAction.VolumeDown: binding.DisplayName = _languageService.Get("Hotkey.VolumeDown"); break;
                    case HotKeyAction.Mute: binding.DisplayName = _languageService.Get("Hotkey.Mute"); break;
                    case HotKeyAction.StepBackward01s: binding.DisplayName = string.Format(_languageService.Get("Hotkey.StepBackward"), _editableSettings.StepDurationSeconds.ToString("0.##")); break;
                    case HotKeyAction.StepForward01s: binding.DisplayName = string.Format(_languageService.Get("Hotkey.StepForward"), _editableSettings.StepDurationSeconds.ToString("0.##")); break;
                    case HotKeyAction.GoToStart: binding.DisplayName = _languageService.Get("Hotkey.GoToStart"); break;
                    case HotKeyAction.GoToEnd: binding.DisplayName = _languageService.Get("Hotkey.GoToEnd"); break;
                    case HotKeyAction.SpeedDecrease: binding.DisplayName = string.Format(_languageService.Get("Hotkey.SpeedDecrease"), _editableSettings.SpeedChangePercent); break;
                    case HotKeyAction.SpeedIncrease: binding.DisplayName = string.Format(_languageService.Get("Hotkey.SpeedIncrease"), _editableSettings.SpeedChangePercent); break;
                    case HotKeyAction.SpeedReset: binding.DisplayName = string.Format(_languageService.Get("Hotkey.SpeedReset"), _editableSettings.SpeedResetPercent); break;
                    case HotKeyAction.ToggleLoopMode: binding.DisplayName = _languageService.Get("Hotkey.ToggleLoop"); break;
                    case HotKeyAction.ToggleWaveform: binding.DisplayName = _languageService.Get("Hotkey.ToggleWaveform"); break;
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
            DisplayCategoryItem.Content = _languageService.Get("Settings.Display");
            HotkeysCategoryItem.Content = _languageService.Get("Settings.Hotkeys");
            NumericCategoryItem.Content = _languageService.Get("Settings.Numeric");
            GeneralTitleText.Text = _languageService.Get("Settings.GeneralTitle");
            GeneralHintText.Text = _languageService.Get("Settings.GeneralHint");
            LanguageLabelText.Text = _languageService.Get("Settings.Language");
            ThemeLabelText.Text = _languageService.Get("Settings.Theme");
            RememberLastVolumeCheck.Content = _languageService.Get("Settings.RememberLastVolume");
            RememberLastSpeedCheck.Content = _languageService.Get("Settings.RememberLastSpeed");
            RememberLastLoopCheck.Content = _languageService.Get("Settings.RememberLastLoop");
            LightThemeItem.Content = _languageService.Get("Theme.Light");
            DarkThemeItem.Content = _languageService.Get("Theme.Dark");
            SystemThemeItem.Content = _languageService.Get("Theme.System");
            MaxVolumeLabelText.Text = _languageService.Get("Settings.MaxVolume");
            DefaultVolumeLabelText.Text = _languageService.Get("Settings.DefaultVolume");
            DisplayTitleText.Text = _languageService.Get("Settings.DisplayTitle");
            DisplayHintText.Text = _languageService.Get("Settings.DisplayHint");
            ShowWaveformLabelText.Text = _languageService.Get("Settings.ShowWaveform");
            HotkeysTitleText.Text = _languageService.Get("Settings.HotkeysTitle");
            HotkeysHintText.Text = _languageService.Get("Settings.HotkeysHint");
            SkipDurationLabelText.Text = _languageService.Get("Settings.SkipDuration");
            SmallSkipDurationLabelText.Text = _languageService.Get("Settings.StepDuration");
            ScrollVolumeLabelText.Text = _languageService.Get("Settings.ScrollVolume");
            HotkeyVolumeLabelText.Text = _languageService.Get("Settings.VolumeStep");
            SpeedStepLabelText.Text = _languageService.Get("Settings.SpeedStep");
            SpeedResetLabelText.Text = _languageService.Get("Settings.SpeedReset");
            MaxRecentFilesLabelText.Text = _languageService.Get("Settings.MaxRecentFiles");
            NumericTitleText.Text = _languageService.Get("Settings.NumericTitle");
            NumericHintText.Text = _languageService.Get("Settings.NumericHint");
            JapaneseLanguageItem.Content = _languageService.Get("Language.Japanese");
            EnglishLanguageItem.Content = _languageService.Get("Language.EnglishUS");
            CancelButton.Content = _languageService.Get("Common.Cancel");
            ApplyButton.Content = _languageService.Get("Common.Apply");
            OkButton.Content = _languageService.Get("Common.OK");
            UpdateHotKeyDisplayNames();
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
            DisplayPanel.Visibility = tag == "Display" ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
