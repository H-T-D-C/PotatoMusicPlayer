namespace PotatoMusicPlayer.Models
{
    public enum CursorDisplayMode
    {
        CenterFixed = 0,
        LeftScroll = 1
    }

    /// <summary>
    /// 波形に表示する時間範囲とメディア全体の情報。
    /// </summary>
    public class WaveformZoomState
    {
        public double CurrentZoomLevel { get; set; }
        public double VisibleRangeStart { get; set; }
        public double VisibleRangeEnd { get; set; }
        public double TotalDuration { get; set; }
        public double MinZoomLevel { get; set; }
        public double MaxZoomLevel { get; set; }
        public double CurrentPlaybackPosition { get; set; }

        public double VisibleRangeDuration => VisibleRangeEnd - VisibleRangeStart;
        public double ZoomRatio => CurrentZoomLevel > 0 ? TotalDuration / CurrentZoomLevel : 1;
    }
}
