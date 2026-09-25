using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;
using PotatoMusicPlayer.Models;
using PotatoMusicPlayer.Services;
using PotatoMusicPlayer.ViewModels;
using PotatoMusicPlayer.Views;

namespace PotatoMusicPlayer
{
    public partial class MainWindow : Window
    {
        private enum MinimapDragMode
        {
            None,
            MoveRange,
            PointToRange,
            ResizeStart,
            ResizeEnd,
            ZoomAroundCenter
        }

        private readonly MainViewModel _viewModel;
        private readonly LanguageService _languageService;
        private bool _isDraggingSeekBar = false;
        private bool _wasPlayingBeforeSeekBarDrag;
        private bool _isDraggingWaveform = false;
        private bool _isPotentialWaveformPan = false;
        private bool _isPanningWaveform = false;
        private bool _isCenterWaveformPanMode;
        private bool _keepWaveformRangeDuringDrag;
        private bool _resumePlaybackAfterWaveformDrag;
        private bool _wasPlayingBeforeWaveformDrag;
        private bool _resumePlaybackAfterMinimapDrag;
        private bool _wasPlayingBeforeMinimapDrag;
        private bool _isUpdatingVolumeFromCode = false;
        private float[] _waveformData = Array.Empty<float>();
        private readonly List<Rectangle> _waveformBars = new();
        private Line _playbackCursorLine;
        private Line _centerGuideLine;
        private double _waveformPointerStartX;
        private double _waveformPanInitialRangeStart;
        private double _waveformPanInitialRangeDuration;
        private double _playbackAnchorPosition;
        private long _playbackAnchorTimestamp;
        private float _playbackAnchorSpeed = 1.0f;
        private bool _isPlaybackRenderingAttached;
        private double _drawnWaveformRangeStart;
        private double _drawnWaveformRangeDuration;
        private bool _hasDrawnWaveformRange;
        private bool _showWaveformRangeAsPercentage;
        private MinimapDragMode _minimapDragMode;
        private double _minimapDragStartX;
        private double _minimapDragStartTime;
        private double _minimapInitialRangeStart;
        private double _minimapInitialRangeEnd;
        private double _minimapInitialPosition;
        private double _minimapPreviousExcess;
        private bool _minimapDragStarted;
        private Line _minimapCursorLine;
        private Rectangle _minimapRangeOverlay;
        private Rectangle _minimapLeftHandle;
        private Rectangle _minimapRightHandle;
        private bool _isZoomingFromRightHandle;
        private float[] _minimapWaveformCache;
        private double _minimapCachedWidth;
        private double _minimapCachedHeight;

        public MainWindow()
        {
            InitializeComponent();

            ThemeService.ThemeChanged += ThemeService_ThemeChanged;

            _viewModel = new MainViewModel();
            DataContext = _viewModel;
            _languageService = new LanguageService(_viewModel.Settings.Language);
            ApplyLanguage();

            // ViewModel のプロパティ変更を UI に反映
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;

            // ウィンドウ設定を復元
            RestoreWindowSettings();
            UpdateVolumeIcon(VolumeSlider.Value);

            // ホットキー処理
            PreviewKeyDown += MainWindow_PreviewKeyDown;

            // Recent files メニューを初期化
            UpdateRecentFilesMenu();

            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            string startupFile = App.StartupFilePath;
            if (string.IsNullOrEmpty(startupFile) || !FileService.FileExists(startupFile))
                return;

            await _viewModel.LoadAndPlayFileAsync(startupFile);
            UpdateRecentFilesMenu();
        }

        private void ThemeService_ThemeChanged(object sender, EventArgs e)
        {
            UpdateVolumeIcon(_viewModel?.PlaybackState?.IsMuted == true ? 0 : VolumeSlider?.Value ?? 0);
            _minimapWaveformCache = null;
            DrawWaveform();
            DrawMinimap();
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
            CenterFixedWaveformMenuItem.IsChecked = settings.WaveformZoom.CursorMode == CursorDisplayMode.CenterFixed;
            WaveformContainer.Visibility = settings.ShowWaveform ? Visibility.Visible : Visibility.Collapsed;
            ApplyWaveformSettingsToUi();
            VolumeSlider.Maximum = Math.Max(100, settings.MaxVolumeMultiplier * 100.0);

            if (settings.IsWindowSizeFixed)
            {
                ResizeMode = ResizeMode.NoResize;
                FixWindowSizeMenuItem.IsChecked = true;
            }

            VolumeSlider.Value = Math.Clamp(settings.DefaultVolume * 100, VolumeSlider.Minimum, VolumeSlider.Maximum);
            UpdateThemeMenuSelection(settings.Theme);
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SetPlaybackRendering(false);
            ThemeService.ThemeChanged -= ThemeService_ThemeChanged;
            // ウィンドウの状態を保存
            var settings = _viewModel.Settings;
            if (settings.RememberLastVolume)
                settings.DefaultVolume = (float)Math.Clamp(VolumeSlider.Value / 100.0, 0.0, settings.MaxVolumeMultiplier);
            if (settings.RememberLastPlaybackSpeed && _viewModel.PlaybackState != null)
                settings.DefaultPlaybackSpeed = Math.Clamp(_viewModel.PlaybackState.PlaybackSpeed, 0.25f, 4.0f);
            if (settings.RememberLastLoopMode && _viewModel.PlaybackState != null)
                settings.DefaultLoopMode = _viewModel.PlaybackState.LoopMode;
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
                    DrawMinimap();
                }
                else if (e.PropertyName == nameof(MainViewModel.ZoomState))
                {
                    bool isAutomaticCenterFollow = _viewModel.Settings.WaveformZoom.CursorMode == CursorDisplayMode.CenterFixed &&
                        _viewModel.PlaybackState?.State == PlayState.Playing &&
                        !_isDraggingWaveform && _minimapDragMode == MinimapDragMode.None;
                    if (!isAutomaticCenterFollow)
                    {
                        if (_viewModel.Settings.WaveformZoom.CursorMode == CursorDisplayMode.CenterFixed)
                        {
                            RefreshWaveformRange();
                            DrawPlaybackCursorAtCenter();
                        }
                        else
                            DrawWaveform();
                    }
                    DrawMinimap();
                    UpdateWaveformRangeDisplay();
                }
                else if (e.PropertyName == nameof(MainViewModel.WaveformProgress))
                {
                    UpdateWaveformProgress();
                }
                else if (e.PropertyName == nameof(MainViewModel.Settings))
                {
                    _languageService.Load(_viewModel.Settings.Language);
                    ApplyLanguage();
                    ApplyAudioSettingsToUi();
                    UpdateThemeMenuSelection(_viewModel.Settings.Theme);
                    ApplyWaveformSettingsToUi();
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

            bool resetPlaybackAnchor = state.State != PlayState.Playing || !_isPlaybackRenderingAttached;
            if (!resetPlaybackAnchor)
            {
                double predictedPosition = _playbackAnchorPosition +
                    Stopwatch.GetElapsedTime(_playbackAnchorTimestamp).TotalSeconds * _playbackAnchorSpeed;
                double correction = state.CurrentPosition.TotalSeconds - predictedPosition;
                // LibVLC の定期通知は描画時刻より遅れて届くことがある。通常の遅れで
                // 基準を巻き戻すと、200msごとにカーソルと波形が逆方向へ跳ねてしまう。
                // 明確なシーク・実測の進みだけを再同期対象にする。
                resetPlaybackAnchor = correction > 0.5 || correction < -0.75 ||
                    Math.Abs(state.PlaybackSpeed - _playbackAnchorSpeed) > 0.001f;
            }
            if (resetPlaybackAnchor)
            {
                _playbackAnchorPosition = state.CurrentPosition.TotalSeconds;
                _playbackAnchorTimestamp = Stopwatch.GetTimestamp();
                _playbackAnchorSpeed = state.PlaybackSpeed;
            }
            SetPlaybackRendering(state.State == PlayState.Playing);
            double displayedPosition = _viewModel.PendingWaveformSeekPosition ?? (state.State == PlayState.Playing
                ? GetInterpolatedPlaybackPosition(state)
                : state.CurrentPosition.TotalSeconds);

            PlayPauseButton.Content = state.State == PlayState.Playing ? "⏸" : "▶";
            TaskbarPlayPauseButton.Description = state.State == PlayState.Playing ? "Pause" : "Play";
            TaskbarPlayPauseButton.ImageSource = state.State == PlayState.Playing
                ? (System.Windows.Media.ImageSource)FindResource("TaskbarPauseIcon")
                : (System.Windows.Media.ImageSource)FindResource("TaskbarPlayIcon");
            CurrentTimeText.Text = FormatTime(state.CurrentPosition);
            SpeedText.Text = $"{state.PlaybackSpeed:0.00}x";
            LoopButton.Content = $"{_languageService.Get("Loop.Label")}: {GetLoopModeDisplayString(state.LoopMode)}";

            // Duration はメディア読み込み直後は 0 のことがあるため、
            // 再生中は毎tickで Maximum を実際の長さに追従させる（シークバー右端張り付き対策）
            if (state.Duration.TotalSeconds > 0)
            {
                if (SeekBar.Maximum != state.Duration.TotalSeconds)
                    SeekBar.Maximum = state.Duration.TotalSeconds;

                TotalTimeText.Text = FormatTime(state.Duration);
            }

            if (!_isDraggingSeekBar && !_isDraggingWaveform &&
                !(state.State == PlayState.Playing && _isPlaybackRenderingAttached))
            {
                SeekBar.Value = state.CurrentPosition.TotalSeconds;
            }

            if (_viewModel.Settings.WaveformZoom.CursorMode == CursorDisplayMode.CenterFixed)
            {
                DrawPlaybackCursorAtCenter(displayedPosition);
            }
            else if (!_isDraggingWaveform)
            {
                DrawPlaybackCursor(TimeSpan.FromSeconds(_minimapCarriedPosition ?? displayedPosition));
            }
            UpdateMinimapCursor(displayedPosition);

            // 音量バーを実際の音量に追従させる（ホットキー操作時も反映）
            _isUpdatingVolumeFromCode = true;
            if (!state.IsMuted)
                VolumeSlider.Value = state.VolumePercent;
            VolumeText.Text = $"{(int)VolumeSlider.Value}%";
            _isUpdatingVolumeFromCode = false;
            UpdateVolumeIcon(state.IsMuted ? 0 : VolumeSlider.Value);
        }

        private void SetPlaybackRendering(bool enabled)
        {
            if (_isPlaybackRenderingAttached == enabled)
                return;

            if (enabled)
                CompositionTarget.Rendering += PlaybackRendering;
            else
                CompositionTarget.Rendering -= PlaybackRendering;

            _isPlaybackRenderingAttached = enabled;
        }

        private void PlaybackRendering(object sender, EventArgs e)
        {
            var playbackState = _viewModel?.PlaybackState;
            var zoomState = _viewModel?.ZoomState;
            if (playbackState?.State != PlayState.Playing || zoomState == null || zoomState.VisibleRangeDuration <= 0)
            {
                SetPlaybackRendering(false);
                return;
            }

            double position = _viewModel.PendingWaveformSeekPosition ??
                GetInterpolatedPlaybackPosition(playbackState);

            if (!_isDraggingSeekBar && !_isDraggingWaveform)
                SeekBar.Value = position;

            if (_viewModel.Settings.WaveformZoom.CursorMode == CursorDisplayMode.CenterFixed)
            {
                // 波形・ミニマップのドラッグ中は描画で競合しないよう、追従の上書きを休止する。
                if (!_isDraggingWaveform && _minimapDragMode == MinimapDragMode.None)
                {
                    _viewModel.FollowWaveformPosition(position);
                    RefreshWaveformRange();
                    DrawPlaybackCursorAtCenter(position);
                }
                UpdateMinimapCursor(position);
                return;
            }

            _viewModel.FollowWaveformPosition(position);
            DrawPlaybackCursor(TimeSpan.FromSeconds(_minimapCarriedPosition ?? position));
            UpdateMinimapCursor(position);
        }

        private double GetInterpolatedPlaybackPosition(PlaybackState state)
        {
            double elapsed = Stopwatch.GetElapsedTime(_playbackAnchorTimestamp).TotalSeconds;
            double position = _playbackAnchorPosition + elapsed * _playbackAnchorSpeed;
            return Math.Clamp(position, 0, Math.Max(0, state.Duration.TotalSeconds));
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
            FileSettingsMenuItem.Header = _languageService.Get("Menu.Edit.Settings");

            // 再生メニュー
            PlaybackPlayPauseMenuItem.Header = _languageService.Get("Menu.Playback.PlayPause");
            PlaybackStopMenuItem.Header = _languageService.Get("Menu.Playback.Stop");
            PlaybackGoToStartMenuItem.Header = _languageService.Get("Menu.Playback.GoToStart");
            PlaybackSeekToTimeMenuItem.Header = _languageService.Get("Menu.Playback.SeekToTime");
            PlaybackSpeedResetMenuItem.Header = _languageService.Get("Menu.Playback.SpeedReset");
            PlaybackSpeedDecreaseMenuItem.Header = _languageService.Get("Menu.Playback.SpeedDecrease");
            PlaybackSpeedIncreaseMenuItem.Header = _languageService.Get("Menu.Playback.SpeedIncrease");
            PlaybackSkipForwardMenuItem.Header = _languageService.Get("Menu.Playback.SkipForward");
            PlaybackSkipBackwardMenuItem.Header = _languageService.Get("Menu.Playback.SkipBackward");

            // 表示メニュー
            AlwaysOnTopMenuItem.Header = _languageService.Get("Menu.View.AlwaysOnTop");
            FixWindowSizeMenuItem.Header = _languageService.Get("Menu.View.FixWindowSize");
            WaveformMenuItem.Header = _languageService.Get("Menu.View.Waveform");
            ShowWaveformMenuItem.Header = _languageService.Get("Menu.View.ShowWaveform");
            CenterFixedWaveformMenuItem.Header = _languageService.Get("Menu.View.CenterFixedWaveform");
            WaveformRangeMenuItem.Header = _languageService.Get("Menu.View.WaveformRange");
            ViewFullScreenMenuItem.Header = _languageService.Get("Menu.View.FullScreen");
            ThemeMenuItem.Header = _languageService.Get("Menu.View.Theme");
            ThemeLightMenuItem.Header = _languageService.Get("Theme.Light");
            ThemeDarkMenuItem.Header = _languageService.Get("Theme.Dark");
            ThemeSystemMenuItem.Header = _languageService.Get("Theme.System");

            // その他メニュー
            OtherAboutMenuItem.Header = _languageService.Get("Menu.Other.About");
            if (_viewModel?.PlaybackState != null)
                LoopButton.Content = $"{_languageService.Get("Loop.Label")}: {GetLoopModeDisplayString(_viewModel.PlaybackState.LoopMode)}";
            UpdateWaveformRangeDisplay();
        }

        private string GetLoopModeDisplayString(LoopMode mode)
        {
            string key = mode switch
            {
                LoopMode.One => "Loop.One",
                LoopMode.All => "Loop.All",
                _ => "Loop.Off"
            };
            return _languageService.Get(key);
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

        private void TaskbarPrevious_Click(object sender, EventArgs e) => _viewModel.SetPosition(0);
        private void TaskbarPlayPause_Click(object sender, EventArgs e) => _viewModel.TogglePlayPause();
        private void TaskbarNext_Click(object sender, EventArgs e) => _viewModel.Stop();

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
        private void SeekToTime_Click(object sender, RoutedEventArgs e)
        {
            if (!InputPromptWindow.TryShow(this, _languageService.Get("Dialog.Seek.Title"), _languageService.Get("Dialog.Seek.Prompt"),
                FormatTime(_viewModel.PlaybackState.CurrentPosition), out string input,
                value => TryParsePosition(value, _viewModel.PlaybackState.Duration.TotalSeconds, out _), _languageService))
                return;

            if (TryParsePosition(input, _viewModel.PlaybackState.Duration.TotalSeconds, out double seconds))
                _viewModel.SetPosition(seconds);
            else
                MessageBox.Show(_languageService.Get("Dialog.Seek.Invalid"), _languageService.Get("Dialog.Seek.Title"), MessageBoxButton.OK, MessageBoxImage.Warning);
        }
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
            _viewModel.Settings.ShowWaveform = ShowWaveformMenuItem.IsChecked;
            WaveformContainer.Visibility = ShowWaveformMenuItem.IsChecked ? Visibility.Visible : Visibility.Collapsed;
            if (ShowWaveformMenuItem.IsChecked)
                DrawWaveform();
        }

        private void CenterFixedWaveform_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.Settings.WaveformZoom.CursorMode = CenterFixedWaveformMenuItem.IsChecked
                ? CursorDisplayMode.CenterFixed
                : CursorDisplayMode.LeftScroll;
            DrawWaveform();
            DrawMinimap();
        }

        private void WaveformRange_Click(object sender, RoutedEventArgs e)
        {
            var zoomState = _viewModel.ZoomState;
            if (zoomState == null || !InputPromptWindow.TryShow(this, _languageService.Get("Dialog.WaveformRange.Title"), _languageService.Get("Dialog.WaveformRange.Prompt"),
                zoomState.VisibleRangeDuration.ToString("0.##"), out string input,
                value => TryParsePosition(value, zoomState.TotalDuration, out double seconds) && seconds > 0, _languageService))
                return;

            if (!TryParsePosition(input, zoomState.TotalDuration, out double seconds) || seconds <= 0)
            {
                MessageBox.Show(_languageService.Get("Dialog.WaveformRange.Invalid"), _languageService.Get("Dialog.WaveformRange.Title"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_viewModel.Settings.WaveformZoom.CursorMode == CursorDisplayMode.CenterFixed)
                _viewModel.ZoomWaveformPreservingRatio(zoomState.VisibleRangeStart,
                    zoomState.VisibleRangeEnd, _viewModel.PlaybackState.CurrentPosition.TotalSeconds, seconds);
            else
            {
                double center = zoomState.VisibleRangeStart + zoomState.VisibleRangeDuration / 2;
                _viewModel.SetWaveformRangeCentered(center, seconds);
            }
        }

        private void ApplyWaveformSettingsToUi()
        {
            if (_viewModel == null || MinimapContainer == null)
                return;

            var appSettings = _viewModel.Settings;
            var settings = appSettings.WaveformZoom;
            ShowWaveformMenuItem.IsChecked = appSettings.ShowWaveform;
            CenterFixedWaveformMenuItem.IsChecked = settings.CursorMode == CursorDisplayMode.CenterFixed;
            WaveformContainer.Visibility = appSettings.ShowWaveform ? Visibility.Visible : Visibility.Collapsed;
            MinimapContainer.Visibility = settings.ShowMinimap ? Visibility.Visible : Visibility.Collapsed;
            int height = Math.Clamp(settings.MinimapHeight, 8, 64);
            MinimapRow.Height = settings.ShowMinimap ? new GridLength(height + 2) : new GridLength(0);
            MinimapContainer.Height = height;
            DrawMinimap();
            UpdateWaveformRangeDisplay();
        }

        private void UpdateWaveformRangeDisplay()
        {
            var zoomState = _viewModel?.ZoomState;
            if (WaveformRangeText == null || zoomState == null)
                return;

            WaveformRangeText.Text = _showWaveformRangeAsPercentage
                ? $"{_languageService.Get("Waveform.RangeLabel")}: {zoomState.VisibleRangeDuration / zoomState.TotalDuration * 100:0.##}%"
                : $"{_languageService.Get("Waveform.RangeLabel")}: {zoomState.VisibleRangeDuration:0.##} {_languageService.Get("Waveform.SecondsUnit")}";
        }

        private void WaveformRangeText_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _showWaveformRangeAsPercentage = !_showWaveformRangeAsPercentage;
            UpdateWaveformRangeDisplay();
            e.Handled = true;
        }

        private static bool TryParsePlaybackTime(string input, out double seconds)
        {
            seconds = 0;
            if (string.IsNullOrWhiteSpace(input))
                return false;

            string[] parts = input.Trim().Split(':');
            if (parts.Length > 3)
                return false;

            double multiplier = 1;
            for (int index = parts.Length - 1; index >= 0; index--)
            {
                if (!double.TryParse(parts[index], out double value) || value < 0 ||
                    (index > 0 && value >= 60))
                    return false;

                seconds += value * multiplier;
                multiplier *= 60;
            }

            return true;
        }

        private static bool TryParsePosition(string input, double totalDuration, out double seconds)
        {
            input = input?.Trim() ?? string.Empty;
            if (input.EndsWith("%") && double.TryParse(input[..^1].Trim(), out double percentage) &&
                percentage >= 0 && percentage <= 100)
            {
                seconds = totalDuration * percentage / 100;
                return true;
            }

            return TryParsePlaybackTime(input, out seconds) && seconds <= totalDuration;
        }

        private void ThemeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem item || !Enum.TryParse(item.Tag?.ToString(), out ThemeMode mode))
                return;

            var settings = _viewModel.Settings;
            settings.Theme = mode;
            new SettingsService().SaveSettings(settings);
            ThemeService.Apply(mode);
            UpdateThemeMenuSelection(mode);
        }

        private void UpdateThemeMenuSelection(ThemeMode mode)
        {
            if (ThemeLightMenuItem == null)
                return;

            ThemeLightMenuItem.IsChecked = mode == ThemeMode.Light;
            ThemeDarkMenuItem.IsChecked = mode == ThemeMode.Dark;
            ThemeSystemMenuItem.IsChecked = mode == ThemeMode.System;
        }

        private void ApplyAudioSettingsToUi()
        {
            if (VolumeSlider == null || _viewModel == null)
                return;

            double maximum = Math.Max(100, _viewModel.Settings.MaxVolumeMultiplier * 100.0);
            double previousValue = VolumeSlider.Value;
            _isUpdatingVolumeFromCode = true;
            VolumeSlider.Maximum = maximum;
            VolumeSlider.Value = Math.Min(previousValue, maximum);
            _isUpdatingVolumeFromCode = false;

            if (previousValue > maximum)
                _viewModel.SetVolume(maximum);
            UpdateVolumeIcon(VolumeSlider.Value);
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

        private void SeekBar_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _wasPlayingBeforeSeekBarDrag = _viewModel.PlaybackState?.State == PlayState.Playing;
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
            if (_wasPlayingBeforeSeekBarDrag)
                _viewModel.SeekAndPlay(SeekBar.Value);
            else
                _viewModel.SetPosition(SeekBar.Value);
            // 中央固定ではシーク位置へ範囲を中央合わせする。
            if (_viewModel.Settings.WaveformZoom.CursorMode == CursorDisplayMode.CenterFixed)
                _viewModel.CenterWaveformRangeOnPosition(SeekBar.Value);
            _wasPlayingBeforeSeekBarDrag = false;
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
            double delta = e.Delta > 0
                ? _viewModel.Settings.ScrollVolumeChangePercent
                : -_viewModel.Settings.ScrollVolumeChangePercent;
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
                new Uri($"pack://application:,,,/Resources/Icons/speaker_{(ThemeService.IsLightMode ? "light" : "dark")}_{level}.png"));
        }

        // ========== ホットキー処理(アプリ内フォーカス時) ==========

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // NOTE: これはアプリがフォーカスされている場合のホットキー。
            Key key = e.Key == Key.System ? e.SystemKey : e.Key;
            var modifiers = Keyboard.Modifiers;
            var binding = _viewModel.Settings.HotKeyBindings.Find(item =>
                item.Key == key && item.Modifiers == modifiers);
            if (binding == null)
                return;

            if (ExecuteHotKeyAction(binding.Action))
                e.Handled = true;
        }

        private bool ExecuteHotKeyAction(HotKeyAction action)
        {
            switch (action)
            {
                case HotKeyAction.PlayPause: _viewModel.TogglePlayPause(); break;
                case HotKeyAction.Stop:
                case HotKeyAction.GoToEnd: _viewModel.Stop(); break;
                case HotKeyAction.SkipBackward5s: _viewModel.SkipBackward(); break;
                case HotKeyAction.SkipForward5s: _viewModel.SkipForward(); break;
                case HotKeyAction.VolumeUp: _viewModel.IncreaseVolume(); break;
                case HotKeyAction.VolumeDown: _viewModel.DecreaseVolume(); break;
                case HotKeyAction.Mute: _viewModel.ToggleMute(); break;
                case HotKeyAction.StepBackward01s:
                    _viewModel.SetPosition(Math.Max(0, _viewModel.PlaybackState.CurrentPosition.TotalSeconds - _viewModel.Settings.StepDurationSeconds));
                    break;
                case HotKeyAction.StepForward01s:
                    _viewModel.SetPosition(_viewModel.PlaybackState.CurrentPosition.TotalSeconds + _viewModel.Settings.StepDurationSeconds);
                    break;
                case HotKeyAction.GoToStart: _viewModel.SetPosition(0); break;
                case HotKeyAction.SpeedDecrease: _viewModel.DecreaseSpeed(); break;
                case HotKeyAction.SpeedIncrease: _viewModel.IncreaseSpeed(); break;
                case HotKeyAction.SpeedReset: _viewModel.ResetSpeed(); break;
                case HotKeyAction.ToggleLoopMode: _viewModel.CycleLoopMode(); break;
                case HotKeyAction.ToggleWaveform:
                    ShowWaveformMenuItem.IsChecked = !ShowWaveformMenuItem.IsChecked;
                    ShowWaveform_Click(this, new RoutedEventArgs());
                    break;
                case HotKeyAction.WaveformZoomIn: _viewModel.ZoomIn(); break;
                case HotKeyAction.WaveformZoomOut: _viewModel.ZoomOut(); break;
                default: return false;
            }

            return true;
        }
    }
}
