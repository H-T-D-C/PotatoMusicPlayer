using System;
using PotatoMusicPlayer.Models;

namespace PotatoMusicPlayer.Services
{
    /// <summary>
    /// 波形の表示範囲とズーム量を計算する。
    /// </summary>
    public class WaveformZoomService
    {
        public const double DefaultMinimumZoomLevel = 1.0;
        public const double DefaultZoomFactor = 2.0;
        public const double DefaultInitialZoomRatio = 60.0;

        private double _minimumZoomLevel = DefaultMinimumZoomLevel;
        private double _zoomFactor = DefaultZoomFactor;

        public void Configure(WaveformZoomSettings settings)
        {
            settings ??= new WaveformZoomSettings();
            _minimumZoomLevel = Math.Clamp(settings.MinZoomLevel, 0.05, 60);
            _zoomFactor = Math.Clamp(settings.ZoomFactor, 1.1, 10);
        }

        public WaveformZoomState CreateInitialState(double totalDuration)
        {
            totalDuration = Math.Max(0, totalDuration);
            if (totalDuration <= 0)
            {
                return new WaveformZoomState
                {
                    MinZoomLevel = 0,
                    MaxZoomLevel = 0
                };
            }

            double minimumZoomLevel = Math.Min(_minimumZoomLevel, totalDuration);
            double initialZoomLevel = Math.Clamp(totalDuration / DefaultInitialZoomRatio,
                minimumZoomLevel, totalDuration);

            return new WaveformZoomState
            {
                CurrentZoomLevel = initialZoomLevel,
                VisibleRangeStart = 0,
                VisibleRangeEnd = initialZoomLevel,
                TotalDuration = totalDuration,
                MinZoomLevel = minimumZoomLevel,
                MaxZoomLevel = totalDuration
            };
        }

        public void ZoomInAroundPoint(WaveformZoomState state, double pivotTime)
        {
            ZoomAroundPoint(state, state.CurrentZoomLevel / _zoomFactor, pivotTime);
        }

        public void ZoomOutAroundPoint(WaveformZoomState state, double pivotTime)
        {
            ZoomAroundPoint(state, state.CurrentZoomLevel * _zoomFactor, pivotTime);
        }

        /// <summary>
        /// 再生位置の範囲内での相対割合を保って拡大縮小し、位置を動かさない。
        /// 端では丸められるため割合が崩れる場合がある。
        /// </summary>
        public void ZoomInPreservingRatio(WaveformZoomState state, double position)
        {
            ZoomPreservingRatio(state, state.CurrentZoomLevel / _zoomFactor, position);
        }

        public void ZoomOutPreservingRatio(WaveformZoomState state, double position)
        {
            ZoomPreservingRatio(state, state.CurrentZoomLevel * _zoomFactor, position);
        }

        public void ZoomPreservingRatio(WaveformZoomState state, double requestedWidth, double position)
        {
            if (state == null || state.TotalDuration <= 0)
                return;

            double width = Math.Clamp(requestedWidth, state.MinZoomLevel, state.MaxZoomLevel);
            double ratio = state.CurrentZoomLevel > 0
                ? Math.Clamp((Math.Clamp(position, 0, state.TotalDuration) - state.VisibleRangeStart) / state.CurrentZoomLevel, 0, 1)
                : 0.5;
            SetRangeAllowingEdges(state, position - ratio * width, width);
        }

        public void ZoomPreservingRatio(WaveformZoomState state, double rangeStart, double rangeEnd,
            double position, double requestedWidth)
        {
            if (state == null || state.TotalDuration <= 0 || rangeEnd <= rangeStart)
                return;

            double width = Math.Clamp(requestedWidth, state.MinZoomLevel, state.MaxZoomLevel);
            double ratio = Math.Clamp((position - rangeStart) / (rangeEnd - rangeStart), 0, 1);
            SetRangeAllowingEdges(state, position - ratio * width, width);
        }

        public void CenterOnPositionAllowingEdges(WaveformZoomState state, double position)
        {
            if (state == null || state.TotalDuration <= 0)
                return;

            double width = state.CurrentZoomLevel;
            double boundedPosition = Math.Clamp(position, 0, state.TotalDuration);
            SetRangeAllowingEdges(state, boundedPosition - width / 2, width);
        }

        public void FollowCenterFixed(WaveformZoomState state, double position)
        {
            if (state == null || state.VisibleRangeDuration <= 0)
                return;

            if (ShouldHoldCenterFixed(state, position))
                return;

            CenterOnPositionAllowingEdges(state, position);
        }

        public bool ShouldHoldCenterFixed(WaveformZoomState state, double position)
        {
            if (state == null || state.VisibleRangeDuration <= 0)
                return false;

            double center = (state.VisibleRangeStart + state.VisibleRangeEnd) / 2;
            bool atTrackStart = position <= 0.001 && state.VisibleRangeStart >= -0.001;
            return !atTrackStart && position >= state.VisibleRangeStart && position < center;
        }

        public (double Start, double End) GetVisibleRangeWithinTrack(WaveformZoomState state)
        {
            if (state == null || state.TotalDuration <= 0)
                return (0, 0);

            return (Math.Clamp(state.VisibleRangeStart, 0, state.TotalDuration),
                Math.Clamp(state.VisibleRangeEnd, 0, state.TotalDuration));
        }

        public void ZoomAroundPoint(WaveformZoomState state, double requestedWidth, double pivotTime)
        {
            if (state == null || state.TotalDuration <= 0)
                return;

            double width = Math.Clamp(requestedWidth, state.MinZoomLevel, state.MaxZoomLevel);
            SetRangeAroundCenter(state, pivotTime, width);
        }

        public void Scroll(WaveformZoomState state, double seconds)
        {
            if (state == null || state.TotalDuration <= 0)
                return;

            double maxStart = Math.Max(0, state.TotalDuration - state.CurrentZoomLevel);
            state.VisibleRangeStart = Math.Clamp(state.VisibleRangeStart + seconds, 0, maxStart);
            state.VisibleRangeEnd = state.VisibleRangeStart + state.CurrentZoomLevel;
        }

        public void SetVisibleRange(WaveformZoomState state, double startTime, double endTime)
        {
            if (state == null || state.TotalDuration <= 0)
                return;

            double width = Math.Clamp(endTime - startTime, state.MinZoomLevel, state.MaxZoomLevel);
            SetRangeAroundCenter(state, (startTime + endTime) / 2, width);
        }

        public void SetVisibleRangeStart(WaveformZoomState state, double startTime)
        {
            if (state == null)
                return;

            double end = state.VisibleRangeEnd;
            double start = Math.Clamp(startTime, Math.Max(0, end - state.MaxZoomLevel), end - state.MinZoomLevel);
            state.VisibleRangeStart = start;
            state.CurrentZoomLevel = end - start;
        }

        public void SetVisibleRangeEnd(WaveformZoomState state, double endTime)
        {
            if (state == null)
                return;

            double start = state.VisibleRangeStart;
            double end = Math.Clamp(endTime, start + state.MinZoomLevel, Math.Min(state.TotalDuration, start + state.MaxZoomLevel));
            state.VisibleRangeEnd = end;
            state.CurrentZoomLevel = end - start;
        }

        public void SetRangeStart(WaveformZoomState state, double startTime)
        {
            if (state == null || state.TotalDuration <= 0)
                return;

            state.CurrentZoomLevel = Math.Clamp(state.CurrentZoomLevel, state.MinZoomLevel, state.MaxZoomLevel);
            state.VisibleRangeStart = Math.Clamp(startTime, 0, state.TotalDuration - state.CurrentZoomLevel);
            state.VisibleRangeEnd = state.VisibleRangeStart + state.CurrentZoomLevel;
        }

        /// <summary>
        /// 波形ドラッグ用に、曲の範囲外（空白表示）へも移動できるようにする。
        /// 曲の開始・終了地点が中央に来たところで止める。
        /// スクロールや追従は従来通り範囲内へ丸める。
        /// </summary>
        public void PanBeyondEdges(WaveformZoomState state, double startTime)
        {
            if (state == null || state.TotalDuration <= 0)
                return;

            double width = Math.Clamp(state.CurrentZoomLevel, state.MinZoomLevel, state.MaxZoomLevel);
            SetRangeAllowingEdges(state, startTime, width);
        }

        private static void SetRangeAllowingEdges(WaveformZoomState state, double startTime, double width)
        {
            state.CurrentZoomLevel = width;
            state.VisibleRangeStart = Math.Clamp(startTime, -width / 2, state.TotalDuration - width / 2);
            state.VisibleRangeEnd = state.VisibleRangeStart + width;
        }

        public void SetVisibleRangeCentered(WaveformZoomState state, double center, double width)
        {
            if (state == null || state.TotalDuration <= 0)
                return;

            double boundedWidth = Math.Clamp(width, state.MinZoomLevel, state.MaxZoomLevel);
            SetRangeAroundCenter(state, center, boundedWidth);
        }

        private static void SetRangeAroundCenter(WaveformZoomState state, double center, double width)
        {
            width = Math.Clamp(width, state.MinZoomLevel, state.MaxZoomLevel);
            double start = center - width / 2;
            start = Math.Clamp(start, 0, Math.Max(0, state.TotalDuration - width));

            state.CurrentZoomLevel = width;
            state.VisibleRangeStart = start;
            state.VisibleRangeEnd = start + width;
        }
    }
}
