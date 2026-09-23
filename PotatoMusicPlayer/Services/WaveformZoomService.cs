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

        public void ZoomIn(WaveformZoomState state)
        {
            ZoomAroundCenter(state, state.CurrentZoomLevel / _zoomFactor);
        }

        public void ZoomOut(WaveformZoomState state)
        {
            ZoomAroundCenter(state, state.CurrentZoomLevel * _zoomFactor);
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

        public void SetVisibleRangeCentered(WaveformZoomState state, double center, double width)
        {
            if (state == null || state.TotalDuration <= 0)
                return;

            double boundedWidth = Math.Clamp(width, state.MinZoomLevel, state.MaxZoomLevel);
            SetRangeAroundCenter(state, center, boundedWidth);
        }

        private static void ZoomAroundCenter(WaveformZoomState state, double requestedWidth)
        {
            if (state == null || state.TotalDuration <= 0)
                return;

            double center = (state.VisibleRangeStart + state.VisibleRangeEnd) / 2;
            double width = Math.Clamp(requestedWidth, state.MinZoomLevel, state.MaxZoomLevel);
            SetRangeAroundCenter(state, center, width);
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
