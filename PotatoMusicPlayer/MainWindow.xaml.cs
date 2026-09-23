using System;
using System.Windows;
using System.Windows.Controls;
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
        private readonly LanguageService _languageService;
        private bool _isDraggingSeekBar = false;
        private bool _isDraggingWaveform = false;
        private bool _isUpdatingVolumeFromCode = false;
        private float[] _waveformData = Array.Empty<float>();

        public MainWindow()
        {
            InitializeComponent();

            _viewModel = new MainViewModel();
            DataContext = _viewModel;
            _languageService = new LanguageService(_viewModel.Settings.Language);
            ApplyLanguage();

            // ViewModel のプロパティ変更を UI に反映
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;

            // ウィンドウ設定を復元
            RestoreWindowSettings();

            // ホットキー処理
            PreviewKeyDown += MainWindow_PreviewKeyDown;

            // Recent files メニューを初期化
            UpdateRecentFilesMenu();

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
                else if (e.PropertyName == nameof(MainViewModel.CurrentWaveformData))
                {
                    _waveformData = _viewModel.CurrentWaveformData;
                    DrawWaveform();
                }
                else if (e.PropertyName == nameof(MainViewModel.WaveformProgress))
                {
                    UpdateWaveformProgress();
                }
                else if (e.PropertyName == nameof(MainViewModel.Settings))
                {
                    _languageService.Load(_viewModel.Settings.Language);
                    ApplyLanguage();
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
                Title = _languageService.Get("Main.Title");
                return;
            }

            TitleText.Text = string.IsNullOrEmpty(media.Title) ? media.FileName : media.Title;
            ArtistText.Text = !string.IsNullOrEmpty(media.Artist)
                ? $"{media.Artist}  |  {media.Bitrate}kbps  |  {media.SampleRate}Hz"
                : $"{media.Bitrate}kbps | {media.SampleRate}Hz";

            Title = $"{TitleText.Text} - {_languageService.Get("Main.Title")}";
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

            // Duration はメディア読み込み直後は 0 のことがあるため、
            // 再生中は毎tickで Maximum を実際の長さに追従させる（シークバー右端張り付き対策）
            if (state.Duration.TotalSeconds > 0)
            {
                if (SeekBar.Maximum != state.Duration.TotalSeconds)
                    SeekBar.Maximum = state.Duration.TotalSeconds;

                TotalTimeText.Text = FormatTime(state.Duration);
            }

            if (!_isDraggingSeekBar)
            {
                SeekBar.Value = state.CurrentPosition.TotalSeconds;
            }

            if (!_isDraggingWaveform)
            {
                DrawPlaybackCursor(state.CurrentPosition, state.Duration);
            }

            // 音量バーを実際の音量に追従させる（ホットキー操作時も反映）
            _isUpdatingVolumeFromCode = true;
            if (!state.IsMuted)
                VolumeSlider.Value = state.VolumePercent;
            VolumeText.Text = $"{(int)VolumeSlider.Value}%";
            _isUpdatingVolumeFromCode = false;
            UpdateVolumeIcon(state.IsMuted ? 0 : VolumeSlider.Value);
        }

        private string FormatTime(TimeSpan ts)
        {
            return ts.Hours > 0 ? ts.ToString(@"hh\:mm\:ss") : ts.ToString(@"mm\:ss");
        }

        private void ApplyLanguage()
        {
            Title = _languageService.Get("Main.Title");
            FileMenuItem.Header = _languageService.Get("Main.File");
            PlaybackMenuItem.Header = _languageService.Get("Main.Playback");
            EditMenuItem.Header = _languageService.Get("Main.Edit");
            ViewMenuItem.Header = _languageService.Get("Main.View");
            OtherMenuItem.Header = _languageService.Get("Main.Other");
            VolumeIcon.ToolTip = _languageService.Get("Main.VolumeTooltip");
            if (_viewModel.CurrentMediaFile == null)
                TitleText.Text = _languageService.Get("Main.NoFile");

            // ファイルメニュー
            FileOpenMenuItem.Header = _languageService.Get("Menu.File.Open");
            FileOpenLocationMenuItem.Header = _languageService.Get("Menu.File.OpenLocation");
            FileOpenTerminalMenuItem.Header = _languageService.Get("Menu.File.OpenTerminal");
            RecentFilesMenuItem.Header = _languageService.Get("Menu.File.RecentFiles");
            FileExitMenuItem.Header = _languageService.Get("Menu.File.Exit");

            // 再生メニュー
            PlaybackPlayPauseMenuItem.Header = _languageService.Get("Menu.Playback.PlayPause");
            PlaybackStopMenuItem.Header = _languageService.Get("Menu.Playback.Stop");
            PlaybackGoToStartMenuItem.Header = _languageService.Get("Menu.Playback.GoToStart");
            PlaybackSpeedResetMenuItem.Header = _languageService.Get("Menu.Playback.SpeedReset");
            PlaybackSpeedDecreaseMenuItem.Header = _languageService.Get("Menu.Playback.SpeedDecrease");
            PlaybackSpeedIncreaseMenuItem.Header = _languageService.Get("Menu.Playback.SpeedIncrease");
            PlaybackSkipForwardMenuItem.Header = _languageService.Get("Menu.Playback.SkipForward");
            PlaybackSkipBackwardMenuItem.Header = _languageService.Get("Menu.Playback.SkipBackward");

            // 編集メニュー
            EditSettingsMenuItem.Header = _languageService.Get("Menu.Edit.Settings");

            // 表示メニュー
            AlwaysOnTopMenuItem.Header = _languageService.Get("Menu.View.AlwaysOnTop");
            FixWindowSizeMenuItem.Header = _languageService.Get("Menu.View.FixWindowSize");
            ShowWaveformMenuItem.Header = _languageService.Get("Menu.View.ShowWaveform");
            ViewFullScreenMenuItem.Header = _languageService.Get("Menu.View.FullScreen");

            // その他メニュー
            OtherAboutMenuItem.Header = _languageService.Get("Menu.Other.About");
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
                UpdateRecentFilesMenu();
            }
        }

        private void OpenFileLocation_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.CurrentMediaFile != null)
            {
                // FileService はファイルパスも受け取り選択表示するのでそのまま渡す
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
            if (ShowWaveformMenuItem.IsChecked)
                DrawWaveform();
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

        // ========== 波形表示・シーク ==========

        private void WaveformCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawWaveform();
        }

        private void DrawWaveform()
        {
            WaveformCanvas.Children.Clear();

            if (_waveformData == null || _waveformData.Length == 0 ||
                WaveformCanvas.ActualWidth <= 0 || WaveformCanvas.ActualHeight <= 0)
                return;

            // 画面幅に合わせてデータをピーク値でまとめる。バー本体は 1～4px に保つ。
            int visibleBars = Math.Min(_waveformData.Length,
                Math.Max(1, (int)(WaveformCanvas.ActualWidth / 2)));
            double slotWidth = WaveformCanvas.ActualWidth / visibleBars;
            double barWidth = Math.Clamp(slotWidth * 0.75, 1.0, 4.0);
            double availableHeight = Math.Max(1, WaveformCanvas.ActualHeight - 4);
            var waveformBrush = new SolidColorBrush(Color.FromRgb(120, 180, 255));
            waveformBrush.Freeze();

            for (int bar = 0; bar < visibleBars; bar++)
            {
                int start = bar * _waveformData.Length / visibleBars;
                int end = Math.Max(start + 1, (bar + 1) * _waveformData.Length / visibleBars);
                float peak = 0;

                for (int sample = start; sample < end && sample < _waveformData.Length; sample++)
                    peak = Math.Max(peak, _waveformData[sample]);

                // 振幅の中心を波形ボックス中央に置き、上下へ均等に伸ばす。
                double height = Math.Max(1, peak * availableHeight);
                var rectangle = new Rectangle
                {
                    Width = barWidth,
                    Height = height,
                    Fill = waveformBrush,
                    IsHitTestVisible = false
                };

                Canvas.SetLeft(rectangle, bar * slotWidth + (slotWidth - barWidth) / 2);
                Canvas.SetTop(rectangle, (WaveformCanvas.ActualHeight - height) / 2);
                WaveformCanvas.Children.Add(rectangle);
            }

            var state = _viewModel?.PlaybackState;
            if (state != null)
                DrawPlaybackCursor(state.CurrentPosition, state.Duration);
        }

        private void DrawPlaybackCursor(TimeSpan position, TimeSpan duration)
        {
            if (_waveformData == null || _waveformData.Length == 0 ||
                duration.TotalSeconds <= 0 || WaveformCanvas.ActualWidth <= 0)
                return;

            // 波形を再描画せず、前回のカーソル線だけを差し替える。
            for (int i = WaveformCanvas.Children.Count - 1; i >= 0; i--)
            {
                if (WaveformCanvas.Children[i] is Line line && line.Tag as string == "PlaybackCursor")
                    WaveformCanvas.Children.RemoveAt(i);
            }

            double ratio = Math.Clamp(position.TotalSeconds / duration.TotalSeconds, 0, 1);
            double cursorX = ratio * WaveformCanvas.ActualWidth;
            var cursor = new Line
            {
                X1 = cursorX,
                X2 = cursorX,
                Y1 = 0,
                Y2 = WaveformCanvas.ActualHeight,
                Stroke = Brushes.White,
                StrokeThickness = 1,
                Tag = "PlaybackCursor",
                IsHitTestVisible = false
            };
            WaveformCanvas.Children.Add(cursor);
        }

        private void UpdateWaveformProgress()
        {
            double progress = _viewModel.WaveformProgress;
            bool isLoading = progress > 0 && progress < 1;
            WaveformProgressText.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            WaveformProgressText.Text = isLoading ? $"波形を生成中... {(int)(progress * 100)}%" : string.Empty;
        }

        private void WaveformCanvas_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!UpdateWaveformPosition(e))
                return;

            _isDraggingWaveform = true;
            WaveformCanvas.CaptureMouse();
            e.Handled = true;
        }

        private void WaveformCanvas_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingWaveform && e.LeftButton == MouseButtonState.Pressed)
                UpdateWaveformPosition(e);
        }

        private void WaveformCanvas_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDraggingWaveform)
                return;

            if (UpdateWaveformPosition(e))
                _viewModel.Play();

            _isDraggingWaveform = false;
            WaveformCanvas.ReleaseMouseCapture();
            e.Handled = true;
        }

        private bool UpdateWaveformPosition(MouseEventArgs e)
        {
            var duration = _viewModel.PlaybackState?.Duration ?? TimeSpan.Zero;
            if (duration.TotalSeconds <= 0 || WaveformCanvas.ActualWidth <= 0)
                return false;

            double ratio = Math.Clamp(e.GetPosition(WaveformCanvas).X / WaveformCanvas.ActualWidth, 0, 1);
            var position = TimeSpan.FromSeconds(duration.TotalSeconds * ratio);
            DrawPlaybackCursor(position, duration);
            _viewModel.SetPosition(position.TotalSeconds);
            return true;
        }

        // ========== 再生バー(シークバー) ==========

        private void SeekBar_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingSeekBar = true;
            SeekBar.CaptureMouse();
            UpdateSeekBarValueFromMouse(e);
        }

        private void SeekBar_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDraggingSeekBar || e.LeftButton != MouseButtonState.Pressed)
                return;

            UpdateSeekBarValueFromMouse(e);
        }

        private void SeekBar_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDraggingSeekBar = false;
            SeekBar.ReleaseMouseCapture();
            _viewModel.SetPosition(SeekBar.Value);
            _viewModel.Play();
        }

        private void UpdateSeekBarValueFromMouse(MouseEventArgs e)
        {
            // マウス位置からシークバー値を計算して即時反映（UI スレッド）
            var pos = e.GetPosition(SeekBar);
            double relative = SeekBar.ActualWidth > 0 ? pos.X / SeekBar.ActualWidth : 0;
            relative = Math.Max(0.0, Math.Min(1.0, relative));
            double newVal = SeekBar.Minimum + relative * (SeekBar.Maximum - SeekBar.Minimum);
            SeekBar.Value = newVal;
            CurrentTimeText.Text = FormatTime(TimeSpan.FromSeconds(newVal));
        }

        // ========== 音量スライダー ==========

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (VolumeText != null)
            {
                VolumeText.Text = $"{(int)e.NewValue}%";
            }
            UpdateVolumeIcon(e.NewValue);

            // 初期化中やプログラム的な更新時は再反映しない（無限ループ防止）
            if (_isUpdatingVolumeFromCode || _viewModel == null)
                return;

            _viewModel.SetVolume(e.NewValue);
        }

        private void VolumeIcon_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _viewModel.ToggleMute();
            e.Handled = true;
        }

        // 音量領域(アイコン・スライダー・テキスト)上でのマウススクロールで音量を1%ずつ調整
        private void VolumeArea_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double delta = e.Delta > 0 ? 1.0 : -1.0;
            double newValue = Math.Clamp(VolumeSlider.Value + delta, VolumeSlider.Minimum, VolumeSlider.Maximum);

            if (Math.Abs(newValue - VolumeSlider.Value) > 0.001)
            {
                VolumeSlider.Value = newValue;
            }

            e.Handled = true;
        }


        private void UpdateVolumeIcon(double volumePercent)
        {
            if (VolumeIcon == null)
                return;

            int level = volumePercent <= 0
                ? 0
                : Math.Min(3, (int)Math.Ceiling(volumePercent / 50.0));
            VolumeIcon.Source = new System.Windows.Media.Imaging.BitmapImage(
                new Uri($"pack://application:,,,/assets/speaker/speakers_{level}.png"));
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
                case Key.M:
                    _viewModel.ToggleMute();
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
