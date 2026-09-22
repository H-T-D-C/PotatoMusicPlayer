using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using PotatoMusicPlayer.Core;
using PotatoMusicPlayer.Models;
using PotatoMusicPlayer.Services;

namespace PotatoMusicPlayer.ViewModels;

/// <summary>
/// メインウィンドウの ViewModel
/// </summary>
public class MainWindowViewModel : ViewModelBase
{
    private readonly AudioPlayer _audioPlayer;
    private readonly MetadataReader _metadataReader;
    private readonly ConfigService _configService;
    private readonly RecentFilesService _recentFilesService;
    private readonly FileService _fileService;
    private AppSettings _settings;

    // Properties
    private string _currentTitle = "No File";
    private string _currentArtist = "Unknown";
    private string _currentAlbum = "Unknown";
    private string _currentBitrate = "0 kbps";
    private string _currentSampleRate = "0 Hz";
    private TimeSpan _currentPosition;
    private TimeSpan _totalDuration;
    private double _playRate = 1.0;
    private int _volume = 80;
    private bool _isPlaying;
    private bool _isMuted;
    private LoopMode _loopMode = LoopMode.None;
    private string _timeDisplay = "00:00 / 00:00";

    public event EventHandler? PlaybackStateChanged;
    public event EventHandler<string>? ErrorOccurred;

    // Commands
    public ICommand PlayCommand { get; }
    public ICommand PauseCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand OpenFileCommand { get; }
    public ICommand PlayPauseCommand { get; }
    public ICommand SkipForwardCommand { get; }
    public ICommand SkipBackwardCommand { get; }
    public ICommand VolumeUpCommand { get; }
    public ICommand VolumeDownCommand { get; }
    public ICommand MuteCommand { get; }
    public ICommand SpeedUpCommand { get; }
    public ICommand SlowDownCommand { get; }
    public ICommand ResetSpeedCommand { get; }
    public ICommand ToggleLoopCommand { get; }

    public MainWindowViewModel()
    {
        _audioPlayer = new AudioPlayer();
        _metadataReader = new MetadataReader();
        _configService = new ConfigService();
        _recentFilesService = new RecentFilesService();
        _fileService = new FileService();
        _settings = _configService.LoadSettings();

        // オーディオプレイヤーのイベント購読
        _audioPlayer.PlaybackStateChanged += OnPlaybackStateChanged;
        _audioPlayer.PlaybackPositionChanged += OnPlaybackPositionChanged;
        _audioPlayer.MediaEnded += OnMediaEnded;
        _audioPlayer.ErrorOccurred += OnErrorOccurred;

        // 最近使ったファイルを読み込み
        _recentFilesService.Initialize(_settings.RecentFiles.Files);

        // コマンド初期化
        PlayCommand = new RelayCommand(_ => Play());
        PauseCommand = new RelayCommand(_ => Pause());
        StopCommand = new RelayCommand(_ => Stop());
        OpenFileCommand = new RelayCommand(_ => OpenFile());
        PlayPauseCommand = new RelayCommand(_ => PlayPause());
        SkipForwardCommand = new RelayCommand(_ => SkipForward());
        SkipBackwardCommand = new RelayCommand(_ => SkipBackward());
        VolumeUpCommand = new RelayCommand(_ => VolumeUp());
        VolumeDownCommand = new RelayCommand(_ => VolumeDown());
        MuteCommand = new RelayCommand(_ => ToggleMute());
        SpeedUpCommand = new RelayCommand(_ => SpeedUp());
        SlowDownCommand = new RelayCommand(_ => SlowDown());
        ResetSpeedCommand = new RelayCommand(_ => ResetSpeed());
        ToggleLoopCommand = new RelayCommand(_ => ToggleLoop());

        // 初期音量を設定
        if (_settings.Audio.ResetVolumeOnStartup)
        {
            Volume = _settings.Audio.DefaultVolume;
        }

        // 初期再生速度を設定
        if (_settings.Playback.ResetPlayRateOnStartup)
        {
            PlayRate = _settings.Playback.DefaultPlayRate;
        }
    }

    // ===== Command Methods =====

    private void Play()
    {
        _audioPlayer.Play(CurrentTitle == "No File" ? "" : "");
        OnPropertyChanged(nameof(IsPlaying));
    }

    private void Pause()
    {
        _audioPlayer.Pause();
        OnPropertyChanged(nameof(IsPlaying));
    }

    private void Stop()
    {
        _audioPlayer.Stop();
        OnPropertyChanged(nameof(IsPlaying));
    }

    private void PlayPause()
    {
        if (_audioPlayer.IsPlaying)
            Pause();
        else
            Play();
    }

    private void OpenFile()
    {
        var filePath = _fileService.OpenFileDialog();
        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
        {
            LoadFile(filePath);
            _audioPlayer.Play(filePath);
            _recentFilesService.AddFile(filePath);
        }
    }

    private void SkipForward()
    {
        var newPosition = _audioPlayer.CurrentPosition.Add(TimeSpan.FromMilliseconds(_settings.Playback.SkipDuration));
        if (newPosition < _audioPlayer.Duration)
        {
            _audioPlayer.Seek(newPosition);
        }
    }

    private void SkipBackward()
    {
        var newPosition = _audioPlayer.CurrentPosition.Subtract(TimeSpan.FromMilliseconds(_settings.Playback.SkipDuration));
        if (newPosition < TimeSpan.Zero)
            newPosition = TimeSpan.Zero;

        _audioPlayer.Seek(newPosition);
    }

    private void VolumeUp()
    {
        Volume = Math.Min(Volume + 5, _settings.Audio.MaxVolume);
    }

    private void VolumeDown()
    {
        Volume = Math.Max(Volume - 5, 0);
    }

    private void ToggleMute()
    {
        _audioPlayer.ToggleMute();
        IsMuted = _audioPlayer.IsMuted;
    }

    private void SpeedUp()
    {
        PlayRate = Math.Min(PlayRate + _settings.Playback.SpeedStep, 2.0);
    }

    private void SlowDown()
    {
        PlayRate = Math.Max(PlayRate - _settings.Playback.SpeedStep, 0.25);
    }

    private void ResetSpeed()
    {
        PlayRate = 1.0;
    }

    private void ToggleLoop()
    {
        LoopMode = LoopMode switch
        {
            LoopMode.None => LoopMode.One,
            LoopMode.One => LoopMode.All,
            LoopMode.All => LoopMode.None,
            _ => LoopMode.None
        };
    }

    private void LoadFile(string filePath)
    {
        var mediaInfo = _metadataReader.ReadMetadata(filePath);
        CurrentTitle = mediaInfo.Title;
        CurrentArtist = mediaInfo.Artist;
        CurrentAlbum = mediaInfo.Album;
        CurrentBitrate = $"{mediaInfo.BitRate} kbps";
        CurrentSampleRate = $"{mediaInfo.SampleRate} Hz";
        TotalDuration = mediaInfo.Duration;
    }

    // ===== Event Handlers =====

    private void OnPlaybackStateChanged(object? sender, EventArgs e)
    {
        IsPlaying = _audioPlayer.IsPlaying;
        IsMuted = _audioPlayer.IsMuted;
        PlayRate = _audioPlayer.PlayRate;
        Volume = _audioPlayer.Volume;
        PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnPlaybackPositionChanged(object? sender, EventArgs e)
    {
        CurrentPosition = _audioPlayer.CurrentPosition;
        UpdateTimeDisplay();
    }

    private void OnMediaEnded(object? sender, EventArgs e)
    {
        if (_loopMode == LoopMode.One)
        {
            _audioPlayer.Seek(TimeSpan.Zero);
            _audioPlayer.Play(_audioPlayer.CurrentMediaPath ?? "");
        }
        else if (_loopMode == LoopMode.All)
        {
            // プレイリスト機能がないため、同じファイルを再生
            _audioPlayer.Seek(TimeSpan.Zero);
            _audioPlayer.Play(_audioPlayer.CurrentMediaPath ?? "");
        }
    }

    private void OnErrorOccurred(object? sender, string errorMessage)
    {
        ErrorOccurred?.Invoke(this, errorMessage);
    }

    private void UpdateTimeDisplay()
    {
        TimeDisplay = $"{CurrentPosition:mm\\:ss} / {TotalDuration:mm\\:ss}";
    }

    // ===== Properties =====

    public string CurrentTitle
    {
        get => _currentTitle;
        set => SetProperty(ref _currentTitle, value);
    }

    public string CurrentArtist
    {
        get => _currentArtist;
        set => SetProperty(ref _currentArtist, value);
    }

    public string CurrentAlbum
    {
        get => _currentAlbum;
        set => SetProperty(ref _currentAlbum, value);
    }

    public string CurrentBitrate
    {
        get => _currentBitrate;
        set => SetProperty(ref _currentBitrate, value);
    }

    public string CurrentSampleRate
    {
        get => _currentSampleRate;
        set => SetProperty(ref _currentSampleRate, value);
    }

    public TimeSpan CurrentPosition
    {
        get => _currentPosition;
        set => SetProperty(ref _currentPosition, value);
    }

    public TimeSpan TotalDuration
    {
        get => _totalDuration;
        set => SetProperty(ref _totalDuration, value);
    }

    public double PlayRate
    {
        get => _playRate;
        set
        {
            if (SetProperty(ref _playRate, value))
            {
                _audioPlayer.SetPlayRate(value);
            }
        }
    }

    public int Volume
    {
        get => _volume;
        set
        {
            if (SetProperty(ref _volume, value))
            {
                _audioPlayer.SetVolume(value);
            }
        }
    }

    public bool IsPlaying
    {
        get => _isPlaying;
        set => SetProperty(ref _isPlaying, value);
    }

    public bool IsMuted
    {
        get => _isMuted;
        set => SetProperty(ref _isMuted, value);
    }

    public LoopMode LoopMode
    {
        get => _loopMode;
        set => SetProperty(ref _loopMode, value);
    }

    public string TimeDisplay
    {
        get => _timeDisplay;
        set => SetProperty(ref _timeDisplay, value);
    }

    public void Cleanup()
    {
        _settings.RecentFiles.Files = _recentFilesService.GetRecentFiles();
        _configService.UpdateSettings(_settings);
        _configService.SaveSettings();
        _audioPlayer.Dispose();
    }
}

public enum LoopMode
{
    None,
    One,
    All
}
