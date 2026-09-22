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
        private AppSettings _editableSettings;

        public SettingsWindow(SettingsService settingsService)
        {
            InitializeComponent();
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

            // Deep copy settings to editable instance
            var original = _settingsService.GetSettings();
            var json = JsonConvert.SerializeObject(original);
            _editableSettings = JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();

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

            // default selection
            CategoryList.SelectedIndex = 1; // select "音量" by default
        }

        private void ApplyCurrentSettings()
        {
            // Apply edited values to the original settings instance and save
            _editableSettings.MaxVolumeMultiplier = (float)(MaxVolumeSlider.Value / 100.0);
            _editableSettings.ShowWaveform = ShowWaveformCheck.IsChecked == true;
            _settingsService.SaveSettings(_editableSettings);
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
