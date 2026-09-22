using System.Windows.Media;

namespace PotatoMusicPlayer.Utils
{
    /// <summary>
    /// アプリケーション全体で使用する定数
    /// </summary>
    public static class Constants
    {
        // アプリケーション情報
        public const string AppName = "Potato Music Player";
        public const string AppVersion = "1.0.0";
        public const string AppAuthor = "Your Name";

        // ウィンドウ
        public const double DefaultWindowWidth = 400;
        public const double DefaultWindowHeight = 150;

        // 色定義（ダークテーマ）
        public static class Colors
        {
            public static readonly Color BackgroundDark = Color.FromRgb(30, 30, 30);
            public static readonly Color BackgroundMedium = Color.FromRgb(45, 45, 45);
            public static readonly Color BackgroundLight = Color.FromRgb(60, 60, 60);
            public static readonly Color TextLight = Color.FromRgb(220, 220, 220);
            public static readonly Color TextDim = Color.FromRgb(150, 150, 150);
            public static readonly Color AccentGreen = Color.FromRgb(100, 200, 100);
            public static readonly Color AccentBlue = Color.FromRgb(100, 150, 255);
            public static readonly Color WaveformDefault = Color.FromRgb(120, 180, 255);
        }

        // 再生設定
        public const float MinVolume = 0.0f;
        public const float MaxVolume = 1.0f;
        public const float DefaultVolume = 0.8f;
        public const float VolumeStep = 0.05f;  // 5%

        public const float MinPlaybackSpeed = 0.25f;
        public const float MaxPlaybackSpeed = 2.0f;
        public const float DefaultPlaybackSpeed = 1.0f;
        public const float PlaybackSpeedStep = 0.05f;  // 5%

        // スキップ・ステップ
        public const int DefaultSkipSeconds = 5;
        public const float DefaultStepSeconds = 0.1f;

        // ファイル
        public const int MaxRecentFiles = 50;

        // UI更新頻度
        public const int UIUpdateIntervalMs = 100;  // 100ms ごとに UI 更新
    }
}
