using System;

namespace PotatoMusicPlayer.Models
{
    /// <summary>
    /// 再生状態の列挙型
    /// </summary>
    public enum PlayState
    {
        Stopped = 0,
        Playing = 1,
        Paused = 2
    }

    /// <summary>
    /// ループモードの列挙型
    /// </summary>
    public enum LoopMode
    {
        Off = 0,
        One = 1,      // 1曲ループ
        All = 2       // 全体ループ
    }

    /// <summary>
    /// 再生状態を保持するモデル
    /// </summary>
    public class PlaybackState
    {
        public PlayState State { get; set; } = PlayState.Stopped;
        public TimeSpan CurrentPosition { get; set; } = TimeSpan.Zero;
        public TimeSpan Duration { get; set; } = TimeSpan.Zero;
        public float PlaybackSpeed { get; set; } = 1.0f;  // 1.0 = 100%
        public float Volume { get; set; } = 0.8f;  // 0.0 ~ 1.0
        public LoopMode LoopMode { get; set; } = LoopMode.Off;
        public bool IsMuted { get; set; } = false;
        
        /// <summary>
        /// 再生速度をパーセンテージで取得
        /// </summary>
        public int PlaybackSpeedPercent => (int)(PlaybackSpeed * 100);

        /// <summary>
        /// 音量をパーセンテージで取得
        /// </summary>
        public int VolumePercent => (int)(Volume * 100);

        /// <summary>
        /// 再生状態文字列
        /// </summary>
        public string StateDisplayString => State switch
        {
            PlayState.Playing => "Playing",
            PlayState.Paused => "Paused",
            PlayState.Stopped => "Stopped",
            _ => "Unknown"
        };

    }
}
