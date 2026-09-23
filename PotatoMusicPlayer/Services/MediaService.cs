using System;
using System.Threading;
using System.Threading.Tasks;
using LibVLCSharp.Shared;
using PotatoMusicPlayer.Models;

namespace PotatoMusicPlayer.Services
{
    /// <summary>
    /// LibVLCSharp を使用したメディア再生制御サービス
    /// </summary>
    public class MediaService : IDisposable
    {
        private readonly LibVLC _libVLC;
        private MediaPlayer _mediaPlayer;
        private Media _currentMedia;
        private bool _isInitialized = false;
        private readonly SynchronizationContext _syncContext;
        // ファイル未読み込み状態でも音量設定を保持する（再生開始時に適用される）
        private float _desiredVolume = 0.8f;

        // イベント
        public event EventHandler<TimeSpan> PositionChanged;
        public event EventHandler<TimeSpan> DurationChanged;
        public event EventHandler PlaybackStateChanged;
        public event EventHandler MediaEnded;
        public event EventHandler<string> ErrorOccurred;

        private void Raise(Action action)
        {
            if (_syncContext != null)
            {
                _syncContext.Post(_ => action(), null);
            }
            else
            {
                action();
            }
        }

        public MediaService()
        {
            _syncContext = SynchronizationContext.Current;
            try
            {
                // LibVLCの初期化
                Core.Initialize();
                _libVLC = new LibVLC();
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                Raise(() => ErrorOccurred?.Invoke(this, $"LibVLC initialization failed: {ex.Message}"));
            }
        }

        /// <summary>
        /// ファイルを開いて再生準備
        /// </summary>
        public async Task<bool> LoadFileAsync(string filePath)
        {
            if (!_isInitialized)
                return false;

            try
            {
                // 古いメディアを破棄（UIスレッドで）
                _currentMedia?.Dispose();

                // メディアのパースは LibVLC 側で重い場合があるためバックグラウンドで行う
                var newMedia = await Task.Run(() =>
                {
                    var m = new Media(_libVLC, filePath, FromType.FromPath);
                    m.Parse(MediaParseOptions.ParseLocal);
                    return m;
                });

                // イベントは UI スレッド側で登録する
                _currentMedia = newMedia;
                _currentMedia.ParsedChanged += OnMediaParsedChanged;
                _currentMedia.StateChanged += OnMediaStateChanged;

                if (_mediaPlayer == null)
                {
                    _mediaPlayer = new MediaPlayer(_libVLC);
                    _mediaPlayer.EndReached += OnMediaEnded;
                    _mediaPlayer.TimeChanged += OnMediaTimeChanged;
                    _mediaPlayer.LengthChanged += OnMediaLengthChanged;
                    _mediaPlayer.EncounteredError += OnMediaEncounteredError;
                    // プレイヤー生成前に設定されていた音量を適用
                    _mediaPlayer.Volume = (int)(_desiredVolume * 100);
                }

                _mediaPlayer.Stop();
                _mediaPlayer.Media = _currentMedia;

                return true;
            }
            catch (Exception ex)
            {
                Raise(() => ErrorOccurred?.Invoke(this, $"Failed to load file: {ex.Message}"));
                return false;
            }
        }

        /// <summary>
        /// 再生開始
        /// </summary>
        public void Play()
        {
            try
            {
                if (_mediaPlayer != null)
                {
                    _mediaPlayer.Play();
                    Raise(() => PlaybackStateChanged?.Invoke(this, EventArgs.Empty));
                }
            }
            catch (Exception ex)
            {
                Raise(() => ErrorOccurred?.Invoke(this, $"Play failed: {ex.Message}"));
            }
        }

        /// <summary>
        /// 一時停止
        /// </summary>
        public void Pause()
        {
            try
            {
                if (_mediaPlayer != null && _mediaPlayer.IsPlaying)
                {
                    _mediaPlayer.Pause();
                    Raise(() => PlaybackStateChanged?.Invoke(this, EventArgs.Empty));
                }
            }
            catch (Exception ex)
            {
                Raise(() => ErrorOccurred?.Invoke(this, $"Pause failed: {ex.Message}"));
            }
        }

        /// <summary>
        /// 再生/一時停止の切り替え
        /// </summary>
        public void TogglePlayPause()
        {
            if (_mediaPlayer == null)
                return;

            if (_mediaPlayer.IsPlaying)
                Pause();
            else
                Play();
        }

        /// <summary>
        /// 停止（先頭に戻す）
        /// </summary>
        public void Stop()
        {
            try
            {
                if (_mediaPlayer != null)
                {
                    _mediaPlayer.Stop();
                    _mediaPlayer.Time = 0;
                    Raise(() => PlaybackStateChanged?.Invoke(this, EventArgs.Empty));
                }
            }
            catch (Exception ex)
            {
                Raise(() => ErrorOccurred?.Invoke(this, $"Stop failed: {ex.Message}"));
            }
        }

        /// <summary>
        /// 現在位置を設定（ミリ秒）
        /// </summary>
        public void SetPosition(long milliseconds)
        {
            try
            {
                if (_mediaPlayer != null)
                {
                    _mediaPlayer.Time = milliseconds;
                    Raise(() => PositionChanged?.Invoke(this, TimeSpan.FromMilliseconds(milliseconds)));
                }
            }
            catch (Exception ex)
            {
                Raise(() => ErrorOccurred?.Invoke(this, $"SetPosition failed: {ex.Message}"));
            }
        }

        /// <summary>
        /// 相対位置を移動（秒）
        /// </summary>
        public void SkipRelative(float seconds)
        {
            try
            {
                if (_mediaPlayer != null)
                {
                    long currentMs = _mediaPlayer.Time;
                    long newMs = currentMs + (long)(seconds * 1000);
                    newMs = Math.Max(0, newMs);  // 負にならないように
                    SetPosition(newMs);
                }
            }
            catch (Exception ex)
            {
                Raise(() => ErrorOccurred?.Invoke(this, $"SkipRelative failed: {ex.Message}"));
            }
        }

        /// <summary>
        /// 音量を設定（0.0 ~ maxMultiplier。1.0 = 100%）
        /// LibVLC の Volume プロパティは 100 を超える値（音量ブースト）も受け付ける
        /// </summary>
        public void SetVolume(float volume, float maxMultiplier = 1.0f)
        {
            try
            {
                volume = Math.Clamp(volume, 0.0f, maxMultiplier);
                // 常に希望音量を記録しておき、プレイヤー未生成の間も状態取得で返せるようにする
                _desiredVolume = volume;
                if (_mediaPlayer != null)
                {
                    _mediaPlayer.Volume = (int)(volume * 100);
                }
            }
            catch (Exception ex)
            {
                Raise(() => ErrorOccurred?.Invoke(this, $"SetVolume failed: {ex.Message}"));
            }
        }

        /// <summary>
        /// 再生速度を設定
        /// </summary>
        public void SetPlaybackSpeed(float speed)
        {
            try
            {
                speed = Math.Max(0.25f, speed);  // 0.25倍以上
                if (_mediaPlayer != null)
                {
                    _mediaPlayer.SetRate(speed);
                }
            }
            catch (Exception ex)
            {
                Raise(() => ErrorOccurred?.Invoke(this, $"SetPlaybackSpeed failed: {ex.Message}"));
            }
        }

        /// <summary>
        /// メディア情報を取得（非同期）
        /// </summary>
        public async Task<MediaFile> GetMediaInfoAsync(string filePath)
        {
            var info = new MediaFile
            {
                FilePath = filePath,
                FileName = System.IO.Path.GetFileName(filePath),
                FileFormat = System.IO.Path.GetExtension(filePath).TrimStart('.')
            };

            try
            {
                // 重いパース処理はバックグラウンドで行う
                var duration = await Task.Run(() =>
                {
                    using (var media = new Media(_libVLC, filePath, FromType.FromPath))
                    {
                        media.Parse(MediaParseOptions.ParseLocal);
                        return TimeSpan.FromMilliseconds(media.Duration);
                    }
                });

                info.Duration = duration;
            }
            catch (Exception ex)
            {
                Raise(() => ErrorOccurred?.Invoke(this, $"GetMediaInfo failed: {ex.Message}"));
            }

            return info;
        }

        /// <summary>
        /// 現在の再生状態を取得
        /// </summary>
        public PlaybackState GetPlaybackState()
        {
            var state = new PlaybackState();

            if (_mediaPlayer == null)
            {
                // ファイル未読み込みでも、ユーザー設定済みの音量を反映して返す
                state.Volume = _desiredVolume;
                return state;
            }

            try
            {
                state.State = _mediaPlayer.IsPlaying ? PlayState.Playing : PlayState.Paused;
                state.CurrentPosition = TimeSpan.FromMilliseconds(_mediaPlayer.Time);
                state.Duration = TimeSpan.FromMilliseconds(_mediaPlayer.Length);
                state.Volume = _mediaPlayer.Volume / 100.0f;
                state.PlaybackSpeed = _mediaPlayer.Rate;
            }
            catch (Exception ex)
            {
                Raise(() => ErrorOccurred?.Invoke(this, $"GetPlaybackState failed: {ex.Message}"));
            }

            return state;
        }

        /// <summary>
        /// 再生中かどうか
        /// </summary>
        public bool IsPlaying => _mediaPlayer?.IsPlaying ?? false;

        // ========== Private Event Handlers ==========

        private void OnMediaParsedChanged(object sender, EventArgs e)
        {
            if (_currentMedia != null)
            {
                Raise(() => DurationChanged?.Invoke(this, TimeSpan.FromMilliseconds(_currentMedia.Duration)));
            }
        }

        private void OnMediaStateChanged(object sender, MediaStateChangedEventArgs e)
        {
            Raise(() => PlaybackStateChanged?.Invoke(this, EventArgs.Empty));
        }

        private void OnMediaTimeChanged(object sender, MediaPlayerTimeChangedEventArgs e)
        {
            Raise(() => PositionChanged?.Invoke(this, TimeSpan.FromMilliseconds(e.Time)));
            Raise(() => PlaybackStateChanged?.Invoke(this, EventArgs.Empty));
        }

        private void OnMediaLengthChanged(object sender, MediaPlayerLengthChangedEventArgs e)
        {
            Raise(() => DurationChanged?.Invoke(this, TimeSpan.FromMilliseconds(e.Length)));
            Raise(() => PlaybackStateChanged?.Invoke(this, EventArgs.Empty));
        }

        private void OnMediaEnded(object sender, EventArgs e)
        {
            Raise(() => MediaEnded?.Invoke(this, EventArgs.Empty));
        }

        private void OnMediaEncounteredError(object sender, EventArgs e)
        {
            Raise(() => ErrorOccurred?.Invoke(this, "Media playback error."));
        }

        public void Dispose()
        {
            _currentMedia?.Dispose();
            _mediaPlayer?.Dispose();
            _libVLC?.Dispose();
        }
    }
}
