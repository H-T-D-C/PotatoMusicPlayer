using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;
using PotatoMusicPlayer.Models;
using PotatoMusicPlayer.Services;
using PotatoMusicPlayer.ViewModels;

namespace PotatoMusicPlayer
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private bool _isDraggingSeekBar = false;

        public MainWindow()
        {
            InitializeComponent();

            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            // ViewModel のプロパティ変更を UI に反映
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;

            // ウィンドウ設定を復元
            RestoreWindowSettings();

            // ホットキー処理
            PreviewKeyDown += MainWindow_PreviewKeyDown;

            Closing += MainWindow_Closing;
        }

        // ========== ウィンドウ設定の復元・保存 ==========

        private void RestoreWindowSettings()
        {
            var settings = _viewModel.Settings;
            Width = settings.WindowWidth;
            Height = settings.WindowHeight;
            Left = settings.WindowLeft;
            Top = settings.WindowTop;
            Topmost = settings.IsAlwaysOnTop;
            AlwaysOnTopMenuItem.IsChecked = settings.IsAlwaysOnTop;
            ShowWaveformMenuItem.IsChecked = settings.ShowWaveform;
            WaveformContainer.Visibility = settings.ShowWaveform ? Visibility.Visible : Visibility.Collapsed;

            if (settings.IsWindowSizeFixed)
            {
                ResizeMode = ResizeMode.NoResize;
                FixWindowSizeMenuItem.IsChecked = true;
            }

            VolumeSlider.Value = settings.DefaultVolume * 100;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // ウィンドウの状態を保存
            var settings = _viewModel.Settings;
            settings.WindowWidth = Width;
            settings.WindowHeight = Height;
            settings.WindowLeft = Left;
            settings.WindowTop = Top;
            settings.IsAlwaysOnTop = Topmost;

            var settingsService = new SettingsService();
            settingsService.SaveSettings(settings);

            _viewModel.Dispose();
        }

        // ========== ViewModel との連携 ==========

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (e.PropertyName == nameof(MainViewModel.CurrentMediaFile))
                {
                    UpdateMediaInfoDisplay();
                }
                else if (e.PropertyName == nameof(MainViewModel.PlaybackState))
                {
                    UpdatePlaybackDisplay();
                }
            });
        }

        private void UpdateMediaInfoDisplay()
        {
            var media = _viewModel.CurrentMediaFile;
            if (media == null)
            {
                TitleText.Text = "再生するファイルがありません";
                ArtistText.Text = "";
                Title = "Potato Music Player";
                return;
            }

            TitleText.Text = string.IsNullOrEmpty(media.Title) ? media.FileName : media.Title;
            ArtistText.Text = !string.IsNullOrEmpty(media.Artist)
                ? $"{media.Artist}  |  {media.Bitrate}kbps  |  {media.SampleRate}Hz"
                : $"{media.Bitrate}kbps | {media.SampleRate}Hz";

            Title = $"{TitleText.Text} - Potato Music Player";
            TotalTimeText.Text = FormatTime(media.Duration);
            SeekBar.Maximum = media.Duration.TotalSeconds;
        }

        private void UpdatePlaybackDisplay()
        {
            var state = _viewModel.PlaybackState;
            if (state == null) return;

            PlayPauseButton.Content = state.State == PlayState.Playing ? "⏸" : "▶";
            CurrentTimeText.Text = FormatTime(state.CurrentPosition);
            SpeedText.Text = $"{state.PlaybackSpeed:0.00}x";
            LoopButton.Content = $"Loop: {state.LoopModeDisplayString}";

            if (!_isDraggingSeekBar)
            {
                SeekBar.Value = state.CurrentPosition.TotalSeconds;
            }
        }

        private string FormatTime(TimeSpan ts)
        {
            return ts.Hours > 0 ? ts.ToString(@"hh\:mm\:ss") : ts.ToString(@"mm\:ss");
        }

        // ========== タイトルバー(カスタム) ==========

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            }
            else if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        // ========== メニュー: ファイル ==========

        private async void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = FileService.GetFileDialogFilter(),
                Title = "音楽ファイルを開く"
            };

            if (dialog.ShowDialog() == true)
            {
                await _viewModel.LoadAndPlayFileAsync(dialog.FileName);
            }
        }

        private void OpenFileLocation_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.CurrentMediaFile != null)
            {
                FileService.OpenFolderInExplorer(_viewModel.CurrentMediaFile.FilePath);
            }
        }

        private void OpenInTerminal_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.CurrentMediaFile != null)
            {
                var folder = FileService.GetParentDirectory(_viewModel.CurrentMediaFile.FilePath);
                FileService.OpenFolderInTerminal(folder);
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e) => Close();

        // ========== メニュー: 再生 ==========

        private void PlayPause_Click(object sender, RoutedEventArgs e) => _viewModel.TogglePlayPause();
        private void Stop_Click(object sender, RoutedEventArgs e) => _viewModel.Stop();
        private void GoToStart_Click(object sender, RoutedEventArgs e) => _viewModel.SetPosition(0);
        private void SpeedReset_Click(object sender, RoutedEventArgs e) => _viewModel.ResetSpeed();
        private void SpeedDecrease_Click(object sender, RoutedEventArgs e) => _viewModel.DecreaseSpeed();
        private void SpeedIncrease_Click(object sender, RoutedEventArgs e) => _viewModel.IncreaseSpeed();
        private void SkipForward_Click(object sender, RoutedEventArgs e) => _viewModel.SkipForward();
        private void SkipBackward_Click(object sender, RoutedEventArgs e) => _viewModel.SkipBackward();
        private void ToggleLoop_Click(object sender, RoutedEventArgs e) => _viewModel.CycleLoopMode();

        // ========== メニュー: 編集 ==========

        private void OpenSettings_Click(object sender, RoutedEventArgs e) => _viewModel.OpenSettings();

        // ========== メニュー: 表示 ==========

        private void AlwaysOnTop_Click(object sender, RoutedEventArgs e)
        {
            Topmost = AlwaysOnTopMenuItem.IsChecked;
        }

        private void FixWindowSize_Click(object sender, RoutedEventArgs e)
        {
            ResizeMode = FixWindowSizeMenuItem.IsChecked ? ResizeMode.NoResize : ResizeMode.CanResizeWithGrip;
        }

        private void ShowWaveform_Click(object sender, RoutedEventArgs e)
        {
            WaveformContainer.Visibility = ShowWaveformMenuItem.IsChecked ? Visibility.Visible : Visibility.Collapsed;
        }

        private void FullScreen_Click(object sender, RoutedEventArgs e)
        {
            if (WindowStyle == WindowStyle.None && WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
            else
            {
                WindowState = WindowState.Maximized;
            }
        }

        // ========== メニュー: その他 ==========

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                $"{Utils.Constants.AppName}\nVersion {Utils.Constants.AppVersion}",
                "バージョン情報",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        // ========== 再生バー(シークバー) ==========

        private void SeekBar_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDraggingSeekBar = false;
            _viewModel.SetPosition(SeekBar.Value);
        }

        // ========== 音量スライダー ==========

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (VolumeText != null)
            {
                VolumeText.Text = $"{(int)e.NewValue}%";
            }
            // Slider の値(0-200)を 0.0-2.0 に変換して設定
            // 実際の反映は MediaService 経由で行う想定（ここでは表示のみ簡易実装）
        }

        // ========== ホットキー処理(アプリ内フォーカス時) ==========

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // NOTE: これは「アプリがフォーカスされている場合」のみ有効なホットキー。
            // グローバルホットキー（アプリ非フォーカス時にも効く）は HotKeyService で別途実装。
            switch (e.Key)
            {
                case Key.Space:
                    _viewModel.TogglePlayPause();
                    e.Handled = true;
                    break;
                case Key.Left:
                    _viewModel.SkipBackward();
                    e.Handled = true;
                    break;
                case Key.Right:
                    _viewModel.SkipForward();
                    e.Handled = true;
                    break;
                case Key.Up:
                    _viewModel.IncreaseVolume();
                    e.Handled = true;
                    break;
                case Key.Down:
                    _viewModel.DecreaseVolume();
                    e.Handled = true;
                    break;
                case Key.Home:
                    _viewModel.SetPosition(0);
                    e.Handled = true;
                    break;
                case Key.End:
                    _viewModel.Stop();
                    e.Handled = true;
                    break;
                case Key.A:
                    _viewModel.DecreaseSpeed();
                    e.Handled = true;
                    break;
                case Key.D:
                    _viewModel.IncreaseSpeed();
                    e.Handled = true;
                    break;
                case Key.S:
                    _viewModel.ResetSpeed();
                    e.Handled = true;
                    break;
                case Key.OemComma:
                    _viewModel.SetPosition(Math.Max(0, _viewModel.PlaybackState.CurrentPosition.TotalSeconds - 0.1));
                    e.Handled = true;
                    break;
                case Key.OemPeriod:
                    _viewModel.SetPosition(_viewModel.PlaybackState.CurrentPosition.TotalSeconds + 0.1);
                    e.Handled = true;
                    break;
            }
        }
    }
}
