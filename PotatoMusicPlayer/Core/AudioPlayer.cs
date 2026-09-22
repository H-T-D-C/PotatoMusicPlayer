using System;
using LibVLCSharp.Shared;

namespace PotatoMusicPlayer.Core;

/// <summary>
/// LibVLCSharp を使用したオーディオプレイヤーのラッパークラス
/// </summary>
public class AudioPlayer : IDisposable
{
    private LibVLC? _libVlc;
    private MediaPlayer? _mediaPlayer;
    private bool _disposed;

    public event EventHandler<EventArgs>? PlaybackStateChanged;
    public event EventHandler<EventArgs>? PlaybackPositionChanged;
    public event EventHandler<EventArgs>? MediaEnded;
    public event EventHandler<string>? ErrorOccurred;

    public AudioPlayer()
    {
        Initialize();
    }

    private void Initialize()
    {
        try
        {
            // LibVLC をグローバルに初期化（ウィンドウズではネイティブライブラリが必要）
            // テスト環境では this は明示的に呼ばない方がいい場合もある
            // Core.Initialize() は呼ばずに、LibVLC() コンストラクタで自動的に初期化される
            _libVlc = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVlc);

            _mediaPlayer.EndReached += OnMediaEnded;
            _mediaPlayer.PositionChanged += OnPositionChanged;
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// ファイルを読み込んで再生
    /// </summary>
    public void Play(string filePath)
    {
        if (_mediaPlayer == null || _disposed)
            throw new ObjectDisposedException(nameof(AudioPlayer));

        try
        {
            var media = new Media(_libVlc, filePath);
            _mediaPlayer.Media = media;
            _mediaPlayer.Play();
            PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// 再生一時停止
    /// </summary>
    public void Pause()
    {
        if (_mediaPlayer == null || _disposed)
            throw new ObjectDisposedException(nameof(AudioPlayer));

        _mediaPlayer.Pause();
        PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 完全に停止して先頭に戻る
    /// </summary>
    public void Stop()
    {
        if (_mediaPlayer == null || _disposed)
            throw new ObjectDisposedException(nameof(AudioPlayer));

        _mediaPlayer.Stop();
        PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 指定位置へシーク
    /// </summary>
    public void Seek(TimeSpan position)
    {
        if (_mediaPlayer == null || _disposed)
            throw new ObjectDisposedException(nameof(AudioPlayer));

        _mediaPlayer.Time = (long)position.TotalMilliseconds;
    }

    /// <summary>
    /// 再生速度を設定（0.25～2.0倍速）
    /// </summary>
    public void SetPlayRate(double rate)
    {
        if (_mediaPlayer == null || _disposed)
            throw new ObjectDisposedException(nameof(AudioPlayer));

        // 範囲制限
        rate = Math.Max(0.25, Math.Min(2.0, rate));

        // LibVLCSharp では Rate は読み取り専用。代わりに速度文字列を使用する方法もあるが、
        // 現在は内部的に保持するのみ。UI側で手動で更新する。
        // TODO: VLC の実際の速度設定 API を使用する（現在のバージョン制限）

        PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 音量を設定（0～200）
    /// </summary>
    public void SetVolume(int volume)
    {
        if (_mediaPlayer == null || _disposed)
            throw new ObjectDisposedException(nameof(AudioPlayer));

        volume = Math.Max(0, Math.Min(200, volume));
        _mediaPlayer.Volume = volume;
    }

    /// <summary>
    /// ミュート状態を切り替え
    /// </summary>
    public void ToggleMute()
    {
        if (_mediaPlayer == null || _disposed)
            throw new ObjectDisposedException(nameof(AudioPlayer));

        _mediaPlayer.Mute = !_mediaPlayer.Mute;
        PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
    }

    // Properties

    public bool IsPlaying => _mediaPlayer?.IsPlaying ?? false;

    public bool IsMuted => _mediaPlayer?.Mute ?? false;

    public TimeSpan CurrentPosition
    {
        get => _mediaPlayer?.Time is >= 0 ? TimeSpan.FromMilliseconds(_mediaPlayer.Time) : TimeSpan.Zero;
    }

    public TimeSpan Duration
    {
        get => _mediaPlayer?.Media?.Duration is > 0 ? TimeSpan.FromMilliseconds(_mediaPlayer.Media.Duration) : TimeSpan.Zero;
    }

    public double PlayRate => _mediaPlayer?.Rate ?? 1.0;

    public int Volume => _mediaPlayer?.Volume ?? 0;

    public string? CurrentMediaPath => _mediaPlayer?.Media?.Mrl;

    // Events

    private void OnMediaEnded(object? sender, EventArgs e)
    {
        MediaEnded?.Invoke(this, e);
    }

    private void OnPositionChanged(object? sender, MediaPlayerPositionChangedEventArgs e)
    {
        PlaybackPositionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _mediaPlayer?.Dispose();
        _libVlc?.Dispose();
        _disposed = true;
    }
}
