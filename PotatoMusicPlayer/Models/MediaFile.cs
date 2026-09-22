using System;
using System.Collections.Generic;

namespace PotatoMusicPlayer.Models
{
    /// <summary>
    /// メディアファイル情報を保持するモデル
    /// </summary>
    public class MediaFile
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public string Genre { get; set; }
        public TimeSpan Duration { get; set; }
        public int Bitrate { get; set; }  // kbps
        public int SampleRate { get; set; }  // Hz
        public int Channels { get; set; }
        public string FileFormat { get; set; }  // MP3, FLAC, WAV, OGG etc.

        /// <summary>
        /// メタデータを見やすい形式で返す
        /// </summary>
        public string GetMetadataDisplayString()
        {
            var parts = new List<string>();
            
            if (!string.IsNullOrEmpty(Title))
                parts.Add($"Title: {Title}");
            if (!string.IsNullOrEmpty(Artist))
                parts.Add($"Artist: {Artist}");
            if (!string.IsNullOrEmpty(Album))
                parts.Add($"Album: {Album}");
            if (Bitrate > 0)
                parts.Add($"Bitrate: {Bitrate} kbps");
            if (SampleRate > 0)
                parts.Add($"Sample Rate: {SampleRate} Hz");
            if (Channels > 0)
                parts.Add($"Channels: {Channels}");
            if (!string.IsNullOrEmpty(FileFormat))
                parts.Add($"Format: {FileFormat}");

            return string.Join(" | ", parts);
        }

        public override string ToString()
        {
            return !string.IsNullOrEmpty(Title) ? Title : FileName;
        }
    }
}
