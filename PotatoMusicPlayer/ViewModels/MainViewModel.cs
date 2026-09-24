using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using PotatoMusicPlayer.Models;
using PotatoMusicPlayer.Services;
using PotatoMusicPlayer.Utils;

namespace PotatoMusicPlayer.ViewModels
{
    /// <summary>
    /// メイン画面の ViewModel
    /// UI と ビジネスロジックを分離
    /// </summary>
    public class MainViewModel : ObservableObject
    {
        private readonly MediaService _mediaService;
        private readonly SettingsService _settingsService;
        private readonly WaveformZoomService _waveformZoomService;
        private System.Windows.Threading.DispatcherTimer _updateTimer;
        private int _waveformRequestId;
        private bool _isMuted;
        private float _volumeBeforeMute = 0.8f;
        private float? _volumeStateOverride;
        private bool _isWaveformFollowSuppressed;
        private bool _isWaveformSeekPending;
        private double _pendingWaveformSeekPosition;
        private long _pendingWaveformSeekDeadline;

        // プロパティ
        private MediaFile _currentMediaFile;
        private PlaybackState _playbackState;
        private bool _isLoading;
        private string _statusMessage;
        private float[] _currentWaveformData = Array.Empty<float>();
        private double _waveformProgress;
        private WaveformZoomState _zoomState;

        public MainViewModel()
        {
            _mediaService = new MediaService();
            _settingsService = new SettingsService();
            _waveformZoomService = new WaveformZoomService();

            // イベント登録
            _mediaService.PlaybackStateChanged += (s, e) => UpdatePlaybackState();
            _mediaService.PositionChanged += (s, e) => UpdatePlaybackState();
            _mediaService.DurationChanged += (s, e) => UpdatePlaybackState();
            _mediaService.MediaEnded += (s, e) => OnMediaEnded();
            _mediaService.ErrorOccurred += (s, msg) => StatusMessage = $"Error: {msg}";

            // UI 更新タイマー
            InitializeUpdateTimer();

            // コマンド初期化
            InitializeCommands();

            // 初期状態設定
            PlaybackState = new PlaybackState();
            var startupSettings = _settingsService.GetSettings();
            _waveformZoomService.Configure(startupSettings.WaveformZoom);
            float startupVolume = Math.Clamp(startupSettings.DefaultVolume, 0.0f, startupSettings.MaxVolumeMultiplier);
            float startupSpeed = Math.Clamp(startupSettings.DefaultPlaybackSpeed, 0.25f, 4.0f);
            _volumeBeforeMute = startupVolume;
            _mediaService.SetVolume(startupVolume, startupSettings.MaxVolumeMultiplier);
            _mediaService.SetPlaybackSpeed(startupSpeed);
            PlaybackState.Volume = startupVolume;
            PlaybackState.PlaybackSpeed = startupSpeed;
            PlaybackState.LoopMode = startupSettings.RememberLastLoopMode
                ? startupSettings.DefaultLoopMode
                : LoopMode.Off;
            StatusMessage = "Ready to play music";
        }

        // ========== Public Properties ==========

        public MediaFile CurrentMediaFile
        {
            get => _currentMediaFile;
            set
            {
                if (SetProperty(ref _currentMediaFile, value))
                    ResetWaveformZoom(value?.Duration.TotalSeconds ?? 0);
            }
        }

        public WaveformZoomState ZoomState
        {
            get => _zoomState;
            private set => SetProperty(ref _zoomState, value);
        }

        public PlaybackState PlaybackState
        {
            get => _playbackState;
            set => SetProperty(ref _playbackState, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public float[] CurrentWaveformData
        {
            get => _currentWaveformData;
            private set => SetProperty(ref _currentWaveformData, value);
        }

        public double WaveformProgress
        {
            get => _waveformProgress;
            private set => SetProperty(ref _waveformProgress, value);
        }

        public AppSettings Settings => _settingsService.GetSettings();
        public bool IsMuted => _isMuted;

        // ========== Commands ==========

        public ICommand OpenFileCommand { get; private set; }
        public ICommand PlayPauseCommand { get; private set; }
        public ICommand StopCommand { get; private set; }
        public ICommand SkipForwardCommand { get; private set; }
        public ICommand SkipBackwardCommand { get; private set; }
        public ICommand VolumeUpCommand { get; private set; }
        public ICommand VolumeDownCommand { get; private set; }
        public ICommand SpeedIncreaseCommand { get; private set; }
        public ICommand SpeedDecreaseCommand { get; private set; }
        public ICommand SpeedResetCommand { get; private set; }
        public ICommand ToggleLoopCommand { get; private set; }
        public ICommand SetPositionCommand { get; private set; }
        public ICommand OpenSettingsCommand { get; private set; }

        // ========== Command Implementations ==========

        private void InitializeCommands()
        {
            OpenFileCommand = new RelayCommand(_ => OpenFile());
            PlayPauseCommand = new RelayCommand(_ => TogglePlayPause());
            StopCommand = new RelayCommand(_ => Stop());
            SkipForwardCommand = new RelayCommand(_ => SkipForward());
            SkipBackwardCommand = new RelayCommand(_ => SkipBackward());
            VolumeUpCommand = new RelayCommand(_ => IncreaseVolume());
            VolumeDownCommand = new RelayCommand(_ => DecreaseVolume());
            SpeedIncreaseCommand = new RelayCommand(_ => IncreaseSpeed());
            SpeedDecreaseCommand = new RelayCommand(_ => DecreaseSpeed());
            SpeedResetCommand = new RelayCommand(_ => ResetSpeed());
            ToggleLoopCommand = new RelayCommand(_ => CycleLoopMode());
            SetPositionCommand = new RelayCommand<double>(pos => SetPosition(pos));
            OpenSettingsCommand = new RelayCommand(_ => OpenSettings());
        }

        public void OpenFile()
        {
            IsLoading = true;
            try
            {
                // ファイルダイアログを開く（WPF 実装時に）
                StatusMessage = "Select an audio file...";
                // TODO: FileDialog の実装
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task LoadAndPlayFileAsync(string filePath)
        {
            if (!FileService.IsSupportedFormat(filePath))
            {
                StatusMessage = "Unsupported file format";
                return;
            }

            IsLoading = true;
            try
            {
                bool loaded = await _mediaService.LoadFileAsync(filePath);
                if (loaded)
                {
                    CurrentMediaFile = await _mediaService.GetMediaInfoAsync(filePath);
                    _settingsService.AddRecentFile(filePath);
                    Play();
                    StatusMessage = $"Loaded: {CurrentMediaFile.FileName}";
                    _ = LoadWaveformAsync(filePath);
                }
                else
                {
                    StatusMessage = "Failed to load file";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                Debug.WriteLine(ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public void TogglePlayPause()
        {
            if (PlaybackState?.State == PlayState.Playing)
            {
                _mediaService.Pause();
                UpdatePlaybackState();
                return;
            }

            Play();
        }

        public void Play()
        {
            _isWaveformFollowSuppressed = false;
            // MediaService resets LibVLC's Ended state when necessary. Do not
            // use the cached UI position here, since it can be stale after a seek.
            _mediaService.Play();
            UpdatePlaybackState();
        }

        public void PlayKeepingWaveformRange()
        {
            _mediaService.Play();
            UpdatePlaybackState();
        }

        public void Pause()
        {
            _mediaService.Pause();
            UpdatePlaybackState();
        }

        public void Stop()
        {
            _mediaService.Stop();
            UpdatePlaybackState();
        }

        public void SkipForward()
        {
            float skipSeconds = _settingsService.GetSettings().SkipDurationSeconds;
            _mediaService.SkipRelative(skipSeconds);
        }

        public void SkipBackward()
        {
            float skipSeconds = _settingsService.GetSettings().SkipDurationSeconds;
            _mediaService.SkipRelative(-skipSeconds);
        }

        public void IncreaseVolume()
        {
            var settings = _settingsService.GetSettings();
            if (_isMuted)
                SetMuted(false);
            float change = settings.VolumeChangePercent / 100.0f;
            float maxVolume = settings.MaxVolumeMultiplier;
            float newVolume = Math.Min(PlaybackState.Volume + change, maxVolume);
            _mediaService.SetVolume(newVolume, maxVolume);
            UpdatePlaybackState();
        }

        public void DecreaseVolume()
        {
            var settings = _settingsService.GetSettings();
            if (_isMuted)
                SetMuted(false);
            float change = settings.VolumeChangePercent / 100.0f;
            float newVolume = Math.Max(PlaybackState.Volume - change, 0.0f);
            _mediaService.SetVolume(newVolume, settings.MaxVolumeMultiplier);
            UpdatePlaybackState();
        }

        /// <summary>
        /// 音量をパーセント指定で直接設定する（音量スライダーのドラッグ操作用）
        /// </summary>
        public void SetVolume(double percent)
        {
            var settings = _settingsService.GetSettings();
            float volume = (float)(percent / 100.0);
            volume = Math.Clamp(volume, 0.0f, settings.MaxVolumeMultiplier);
            _isMuted = false;
            _volumeBeforeMute = volume;
            _volumeStateOverride = volume;
            _mediaService.SetVolume(volume, settings.MaxVolumeMultiplier);
            UpdatePlaybackState();
        }

        public void ToggleMute()
        {
            SetMuted(!_isMuted);
        }

        private void SetMuted(bool muted)
        {
            var settings = _settingsService.GetSettings();
            if (muted)
            {
                if (!_isMuted)
                    _volumeBeforeMute = PlaybackState?.Volume ?? _volumeBeforeMute;

                _isMuted = true;
                _mediaService.SetVolume(0, settings.MaxVolumeMultiplier);
            }
            else
            {
                _isMuted = false;
                float restoredVolume = Math.Clamp(_volumeBeforeMute, 0.0f, settings.MaxVolumeMultiplier);
                // LibVLC の状態取得が一瞬だけ旧値(0)を返しても、復元値を先にUIへ渡す。
                _volumeStateOverride = restoredVolume;
                _mediaService.SetVolume(restoredVolume, settings.MaxVolumeMultiplier);
            }

            OnPropertyChanged(nameof(IsMuted));
            UpdatePlaybackState();
        }

        public void IncreaseSpeed()
        {
            var settings = _settingsService.GetSettings();
            float change = settings.SpeedChangePercent / 100.0f;
            float newSpeed = Math.Min(PlaybackState.PlaybackSpeed + change, 2.0f);
            _mediaService.SetPlaybackSpeed(newSpeed);
            UpdatePlaybackState();
        }

        public void DecreaseSpeed()
        {
            var settings = _settingsService.GetSettings();
            float change = settings.SpeedChangePercent / 100.0f;
            float newSpeed = Math.Max(PlaybackState.PlaybackSpeed - change, 0.25f);
            _mediaService.SetPlaybackSpeed(newSpeed);
            UpdatePlaybackState();
        }

        public void ResetSpeed()
        {
            _mediaService.SetPlaybackSpeed(_settingsService.GetSettings().SpeedResetPercent / 100.0f);
            UpdatePlaybackState();
        }

        public void CycleLoopMode()
        {
            PlaybackState.LoopMode = (LoopMode)(((int)PlaybackState.LoopMode + 1) % 3);
            OnPropertyChanged(nameof(PlaybackState));
        }

        public void SetPosition(double seconds)
        {
            CancelPendingWaveformSeek();
            _isWaveformFollowSuppressed = false;
            _mediaService.SetPosition((long)(seconds * 1000));
        }

        public void SetPositionKeepingWaveformRange(double seconds)
        {
            _mediaService.SetPosition((long)(seconds * 1000));
        }

        public void BeginManualWaveformNavigation()
        {
            CancelPendingWaveformSeek();
            _isWaveformFollowSuppressed = true;
        }

        public void EndManualWaveformNavigation()
        {
            _isWaveformFollowSuppressed = _isWaveformSeekPending;
        }

        public void ZoomIn()
        {
            _waveformZoomService.ZoomIn(ZoomState);
            OnPropertyChanged(nameof(ZoomState));
        }

        public void ZoomOut()
        {
            _waveformZoomService.ZoomOut(ZoomState);
            OnPropertyChanged(nameof(ZoomState));
        }

        public void ScrollWaveform(double seconds)
        {
            _waveformZoomService.Scroll(ZoomState, seconds);
            OnPropertyChanged(nameof(ZoomState));
        }

        public void SetWaveformVisibleRange(double startTime, double endTime)
        {
            _waveformZoomService.SetVisibleRange(ZoomState, startTime, endTime);
            OnPropertyChanged(nameof(ZoomState));
        }

        public void SetWaveformRangeStart(double startTime)
        {
            _waveformZoomService.SetVisibleRangeStart(ZoomState, startTime);
            OnPropertyChanged(nameof(ZoomState));
        }

        public void SetWaveformRangeEnd(double endTime)
        {
            _waveformZoomService.SetVisibleRangeEnd(ZoomState, endTime);
            OnPropertyChanged(nameof(ZoomState));
        }

        public void MoveWaveformRange(double startTime)
        {
            _waveformZoomService.SetRangeStart(ZoomState, startTime);
            OnPropertyChanged(nameof(ZoomState));
        }

        public void SetWaveformRangeCentered(double center, double width)
        {
            _waveformZoomService.SetVisibleRangeCentered(ZoomState, center, width);
            OnPropertyChanged(nameof(ZoomState));
        }

        public void ApplyWaveformSettings()
        {
            var previousState = ZoomState;
            double totalDuration = CurrentMediaFile?.Duration.TotalSeconds ?? 0;
            double previousStart = previousState?.VisibleRangeStart ?? 0;
            double previousEnd = previousState?.VisibleRangeEnd ?? 0;
            bool canRestoreRange = previousState != null &&
                previousState.TotalDuration > 0 && totalDuration > 0 &&
                Math.Abs(previousState.TotalDuration - totalDuration) < 0.001 &&
                previousState.VisibleRangeDuration > 0;

            _isWaveformFollowSuppressed = false;
            _waveformZoomService.Configure(Settings.WaveformZoom);

            if (!canRestoreRange)
            {
                ResetWaveformZoom(totalDuration);
                return;
            }

            var restoredState = _waveformZoomService.CreateInitialState(totalDuration);
            _waveformZoomService.SetVisibleRange(restoredState, previousStart, previousEnd);
            ZoomState = restoredState;
        }

        public void FollowWaveformPosition(double position)
        {
            UpdateWaveformFollow(position);
        }

        private void ResetWaveformZoom(double totalDuration)
        {
            CancelPendingWaveformSeek();
            _isWaveformFollowSuppressed = false;
            double previousWidth = ZoomState?.CurrentZoomLevel ?? 0;
            double previousTotalDuration = ZoomState?.TotalDuration ?? 0;
            ZoomState = _waveformZoomService.CreateInitialState(totalDuration);
            if (totalDuration <= 0)
                return;

            double width;
            if (Settings.RememberWaveformZoom && previousWidth > 0)
            {
                width = Settings.DefaultWaveformZoomUnit == WaveformZoomUnit.Percentage && previousTotalDuration > 0
                    ? totalDuration * previousWidth / previousTotalDuration
                    : previousWidth;
            }
            else
            {
                width = Settings.DefaultWaveformZoomUnit == WaveformZoomUnit.Percentage
                    ? totalDuration * Settings.DefaultWaveformZoomValue / 100
                    : Settings.DefaultWaveformZoomValue;
            }

            _waveformZoomService.SetVisibleRangeCentered(ZoomState, Math.Min(width, totalDuration) / 2, width);
        }

        public void SeekAndPlay(double seconds)
        {
            CancelPendingWaveformSeek();
            _isWaveformFollowSuppressed = false;
            _mediaService.PlayFromPosition((long)Math.Max(0, seconds * 1000));
            UpdatePlaybackState();
        }

        public void SeekAndPlayKeepingWaveformRange(double seconds)
        {
            _pendingWaveformSeekPosition = Math.Max(0, seconds);
            _pendingWaveformSeekDeadline = Stopwatch.GetTimestamp() + Stopwatch.Frequency * 3;
            _isWaveformSeekPending = true;
            _isWaveformFollowSuppressed = true;
            _mediaService.PlayFromPosition((long)(_pendingWaveformSeekPosition * 1000));
            UpdatePlaybackState();
        }

        /// <summary>
        /// 再生を妨げずに、表示用のピーク振幅データをバックグラウンドで生成する。
        /// 新しいファイルを読み込んだ場合は、古い要求の結果を破棄する。
        /// </summary>
        public async Task LoadWaveformAsync(string filePath)
        {
            // 画面幅ではなく時間軸の詳細度を確保し、ズームしても波形を確認できるようにする。
            const int barCount = 1_000_000;
            int requestId = Interlocked.Increment(ref _waveformRequestId);
            CurrentWaveformData = Array.Empty<float>();
            WaveformProgress = 0;

            var waveformService = new WaveformService();
            EventHandler<double> progressHandler = (sender, progress) =>
            {
                System.Windows.Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (requestId == _waveformRequestId)
                        WaveformProgress = progress;
                }));
            };
            waveformService.ProgressChanged += progressHandler;

            try
            {
                float[] data = await waveformService.GenerateWaveformAsync(filePath, barCount);
                if (requestId == _waveformRequestId)
                {
                    CurrentWaveformData = data;
                    WaveformProgress = 1;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Waveform generation failed: {ex}");
                if (requestId == _waveformRequestId)
                {
                    CurrentWaveformData = Array.Empty<float>();
                    WaveformProgress = 1;
                }
            }
            finally
            {
                waveformService.ProgressChanged -= progressHandler;
            }
        }

        public void OpenSettings()
        {
            // 設定ウィンドウを開く（UIスレッドで実行される前提）
            try
            {
                var settingsBeforeDialog = _settingsService.GetSettings();
                var window = new PotatoMusicPlayer.Views.SettingsWindow(_settingsService);
                var owner = System.Windows.Application.Current?.MainWindow;
                if (owner != null)
                    window.Owner = owner;

                bool? result = window.ShowDialog();

                // 設定が適用された可能性があるため、プロパティを更新して UI に反映させる
                if (!ReferenceEquals(settingsBeforeDialog, _settingsService.GetSettings()))
                    ApplyWaveformSettings();
                OnPropertyChanged(nameof(Settings));
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to open settings: {ex.Message}";
            }
        }

        // ========== Private Methods ==========

        private void InitializeUpdateTimer()
        {
            _updateTimer = new System.Windows.Threading.DispatcherTimer();
            _updateTimer.Interval = TimeSpan.FromMilliseconds(Constants.UIUpdateIntervalMs);
            _updateTimer.Tick += (s, e) => UpdatePlaybackState();
            _updateTimer.Start();
        }

        private void UpdatePlaybackState()
        {
            var state = _mediaService.GetPlaybackState();
            bool wasPlaying = PlaybackState?.State == PlayState.Playing;
            // Preserve UI-controlled properties (LoopMode) so they are not overwritten by media service snapshot
            if (PlaybackState != null)
            {
                state.LoopMode = PlaybackState.LoopMode;
            }
            state.IsMuted = _isMuted;
            if (_volumeStateOverride.HasValue)
            {
                state.Volume = _volumeStateOverride.Value;
                _volumeStateOverride = null;
            }
            PlaybackState = state;
            UpdatePendingWaveformSeek(state.CurrentPosition.TotalSeconds);
            if (ZoomState != null)
            {
                ZoomState.CurrentPlaybackPosition = state.CurrentPosition.TotalSeconds;
                if (state.State == PlayState.Playing)
                    UpdateWaveformFollow(state.CurrentPosition.TotalSeconds);
                else if (wasPlaying && Settings.WaveformZoom.CursorMode == CursorDisplayMode.CenterFixed)
                    UpdateWaveformFollow(state.CurrentPosition.TotalSeconds);
            }
        }

        private void UpdatePendingWaveformSeek(double position)
        {
            if (!_isWaveformSeekPending)
                return;

            var zoomState = ZoomState;
            bool targetReached = zoomState != null &&
                Math.Abs(position - _pendingWaveformSeekPosition) <= 0.5 &&
                position >= zoomState.VisibleRangeStart &&
                position <= zoomState.VisibleRangeEnd;
            bool timedOut = Stopwatch.GetTimestamp() >= _pendingWaveformSeekDeadline;
            if (targetReached || timedOut)
            {
                CancelPendingWaveformSeek();
                _isWaveformFollowSuppressed = false;
            }
        }

        private void CancelPendingWaveformSeek()
        {
            _isWaveformSeekPending = false;
            _pendingWaveformSeekDeadline = 0;
        }

        private void UpdateWaveformFollow(double position)
        {
            if (_isWaveformFollowSuppressed)
                return;

            var settings = Settings.WaveformZoom;
            var zoomState = ZoomState;
            if (zoomState == null || zoomState.TotalDuration <= 0 || zoomState.VisibleRangeDuration <= 0)
                return;

            if (settings.CursorMode == CursorDisplayMode.CenterFixed)
            {
                double desiredStart = position - zoomState.CurrentZoomLevel / 2;
                desiredStart = Math.Clamp(desiredStart, 0, Math.Max(0, zoomState.TotalDuration - zoomState.CurrentZoomLevel));
                if (Math.Abs(desiredStart - zoomState.VisibleRangeStart) > 0.01)
                    MoveWaveformRange(desiredStart);
                return;
            }

            if (position < zoomState.VisibleRangeStart)
            {
                double pageStart = Math.Floor(position / zoomState.CurrentZoomLevel) * zoomState.CurrentZoomLevel;
                MoveWaveformRange(pageStart);
            }
            else if (position >= zoomState.VisibleRangeEnd)
            {
                double pages = Math.Floor((position - zoomState.VisibleRangeEnd) / zoomState.CurrentZoomLevel) + 1;
                MoveWaveformRange(zoomState.VisibleRangeStart + pages * zoomState.CurrentZoomLevel);
            }
        }

        private async void OnMediaEnded()
        {
            var settings = _settingsService.GetSettings();
            if (settings.RememberLastFile && CurrentMediaFile != null)
            {
                settings.LastPlayedFilePath = CurrentMediaFile.FilePath;
                _settingsService.SaveSettings(settings);
            }

            // ループモードに応じた処理
            if (CurrentMediaFile != null)
            {
                if (PlaybackState.LoopMode == LoopMode.One || PlaybackState.LoopMode == LoopMode.All)
                {
                    // 現在はプレイリスト未実装のため、全体ループも同じ曲の先頭へ戻す。
                    // 左流モードでは表示範囲も先頭ページへ戻す。
                    Stop();
                    SetPosition(0);
                    UpdateWaveformFollow(0);
                    double delaySeconds = Math.Clamp(_settingsService.GetSettings().TrackTransitionDelaySeconds, 0, 60);
                    if (delaySeconds > 0)
                        await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                    Play();
                }
            }

            if (PlaybackState.LoopMode == LoopMode.Off)
            {
                UpdatePlaybackState();
            }

            StatusMessage = "Playback finished";
        }

        public void Dispose()
        {
            Interlocked.Increment(ref _waveformRequestId);
            _updateTimer?.Stop();
            _mediaService?.Dispose();
        }
    }
}
