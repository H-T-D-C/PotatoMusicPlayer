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

            // default selection
            CategoryList.SelectedIndex = 0; // select "一般" by default
        }

        private void ApplyCurrentSettings()
        {
            // Apply edited values to the original settings instance and save
            _editableSettings.MaxVolumeMultiplier = (float)(MaxVolumeSlider.Value / 100.0);
            _editableSettings.ShowWaveform = ShowWaveformCheck.IsChecked == true;
            if (LanguageComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem languageItem &&
                Enum.TryParse(languageItem.Tag?.ToString(), out PotatoMusicPlayer.Models.Language language))
            {
                _editableSettings.Language = language;
            }
            _settingsService.SaveSettings(_editableSettings);
            _languageService.Load(_editableSettings.Language);
            ApplyLanguage();
        }

        private void ApplyLanguage()
        {
            Title = _languageService.Get("Settings.Title");
            GeneralCategoryItem.Content = _languageService.Get("Settings.General");
            VolumeCategoryItem.Content = _languageService.Get("Settings.Volume");
            DisplayCategoryItem.Content = _languageService.Get("Settings.Display");
            GeneralTitleText.Text = _languageService.Get("Settings.GeneralTitle");
            LanguageLabelText.Text = _languageService.Get("Settings.Language");
            VolumeTitleText.Text = _languageService.Get("Settings.VolumeTitle");
            MaxVolumeLabelText.Text = _languageService.Get("Settings.MaxVolume");
            VolumeExampleText.Text = _languageService.Get("Settings.VolumeExample");
            DisplayTitleText.Text = _languageService.Get("Settings.DisplayTitle");
            ShowWaveformLabelText.Text = _languageService.Get("Settings.ShowWaveform");
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
            ApplyCurrentSettings();
            DialogResult = true;
            Close();
        }

        private void CategoryList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var item = CategoryList.SelectedItem as System.Windows.Controls.ListBoxItem;
            if (item == null) return;
            var tag = item.Tag as string ?? "";
            GeneralPanel.Visibility = tag == "General" ? Visibility.Visible : Visibility.Collapsed;
            VolumePanel.Visibility = tag == "Volume" ? Visibility.Visible : Visibility.Collapsed;
            DisplayPanel.Visibility = tag == "Display" ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
