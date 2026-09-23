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
        private System.Windows.Threading.DispatcherTimer _updateTimer;
        private int _waveformRequestId;
        private bool _isMuted;
        private float _volumeBeforeMute = 0.8f;
        private float? _volumeStateOverride;

        // プロパティ
        private MediaFile _currentMediaFile;
        private PlaybackState _playbackState;
        private bool _isLoading;
        private string _statusMessage;
        private float[] _currentWaveformData = Array.Empty<float>();
        private double _waveformProgress;

        public MainViewModel()
        {
            _mediaService = new MediaService();
            _settingsService = new SettingsService();

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
            StatusMessage = "Ready to play music";
        }

        // ========== Public Properties ==========

        public MediaFile CurrentMediaFile
        {
            get => _currentMediaFile;
            set => SetProperty(ref _currentMediaFile, value);
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
            _mediaService.TogglePlayPause();
            UpdatePlaybackState();
        }

        public void Play()
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
            _mediaService.SetPosition((long)(seconds * 1000));
        }

        /// <summary>
        /// 再生を妨げずに、表示用のピーク振幅データをバックグラウンドで生成する。
        /// 新しいファイルを読み込んだ場合は、古い要求の結果を破棄する。
        /// </summary>
        public async Task LoadWaveformAsync(string filePath)
        {
            const int barCount = 1024;
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
                var window = new PotatoMusicPlayer.Views.SettingsWindow(_settingsService);
                var owner = System.Windows.Application.Current?.MainWindow;
                if (owner != null)
                    window.Owner = owner;

                bool? result = window.ShowDialog();

                // 設定が適用された可能性があるため、プロパティを更新して UI に反映させる
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
        }

        private void OnMediaEnded()
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
                if (PlaybackState.LoopMode == LoopMode.One)
                {
                    // 1曲ループ: 停止して先頭に戻し、再生
                    Stop();
                    Play();
                }
                else if (PlaybackState.LoopMode == LoopMode.All)
                {
                    // 全体ループ: プレイリスト未実装のため現状は同様に同一トラックをループ
                    Stop();
                    Play();
                }
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
