using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using PotatoMusicPlayer.Models;

namespace PotatoMusicPlayer.Services
{
    /// <summary>
    /// 波形ピークデータのディスクキャッシュ。
    /// ファイルサイズと更新時刻をキーに検証するため、古いデータを表示しない。
    /// </summary>
    public class WaveformCacheService
    {
        private const uint Magic = 0x504D5057; // "PMPW"
        private const int FormatVersion = 1;
        private const string FileExtension = ".peakcache";

        private readonly long _maxBytes;

        public WaveformCacheService(long maxBytes)
        {
            _maxBytes = Math.Max(1, maxBytes);
        }

        public static string DefaultDirectory => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PotatoMusicPlayer", "WaveformCache");

        public static long ToBytes(double value, CacheSizeUnit unit)
        {
            double multiplier = unit switch
            {
                CacheSizeUnit.KB => 1024.0,
                CacheSizeUnit.GB => 1024.0 * 1024.0 * 1024.0,
                _ => 1024.0 * 1024.0,
            };
            return (long)Math.Clamp(value * multiplier, 1, (double)long.MaxValue);
        }

        public static string FormatSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024L * 1024) return $"{bytes / 1024.0:0.##} KB";
            if (bytes < 1024L * 1024 * 1024) return $"{bytes / (1024.0 * 1024):0.##} MB";
            return $"{bytes / (1024.0 * 1024 * 1024):0.##} GB";
        }

        public bool TryLoad(string filePath, int barCount, out float[] data)
        {
            data = Array.Empty<float>();
            try
            {
                string cachePath = GetCachePath(filePath);
                if (!File.Exists(cachePath))
                    return false;

                var sourceInfo = new FileInfo(filePath);
                using (var stream = File.OpenRead(cachePath))
                using (var reader = new BinaryReader(stream))
                {
                    if (reader.ReadUInt32() != Magic || reader.ReadInt32() != FormatVersion)
                        return false;
                    if (reader.ReadInt64() != sourceInfo.Length)
                        return false;
                    if (reader.ReadInt64() != sourceInfo.LastWriteTimeUtc.Ticks)
                        return false;
                    if (reader.ReadInt32() != barCount)
                        return false;

                    var result = new float[barCount];
                    for (int i = 0; i < barCount; i++)
                        result[i] = reader.ReadSingle();
                    if (stream.Position != stream.Length)
                        return false;

                    data = result;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Waveform cache load failed: {ex.Message}");
                return false;
            }
        }

        public void Save(string filePath, int barCount, float[] data)
        {
            try
            {
                if (data == null || data.Length != barCount)
                    return;

                Directory.CreateDirectory(DefaultDirectory);
                var sourceInfo = new FileInfo(filePath);
                string cachePath = GetCachePath(filePath);

                using (var stream = File.Create(cachePath))
                using (var writer = new BinaryWriter(stream))
                {
                    writer.Write(Magic);
                    writer.Write(FormatVersion);
                    writer.Write(sourceInfo.Length);
                    writer.Write(sourceInfo.LastWriteTimeUtc.Ticks);
                    writer.Write(barCount);
                    foreach (float value in data)
                        writer.Write(value);
                }

                EnforceLimit();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Waveform cache save failed: {ex.Message}");
            }
        }

        public (int files, long bytes) Clear()
        {
            int files = 0;
            long bytes = 0;
            try
            {
                if (!Directory.Exists(DefaultDirectory))
                    return (0, 0);

                foreach (string path in Directory.GetFiles(DefaultDirectory, "*" + FileExtension))
                {
                    try
                    {
                        bytes += new FileInfo(path).Length;
                        File.Delete(path);
                        files++;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Waveform cache delete failed: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Waveform cache clear failed: {ex.Message}");
            }
            return (files, bytes);
        }

        private void EnforceLimit()
        {
            try
            {
                var files = Directory.GetFiles(DefaultDirectory, "*" + FileExtension)
                    .Select(path => new FileInfo(path))
                    .OrderBy(info => info.LastWriteTimeUtc)
                    .ThenBy(info => info.Name)
                    .ToList();

                long total = files.Sum(info => info.Length);
                foreach (var info in files)
                {
                    if (total <= _maxBytes)
                        break;
                    try
                    {
                        total -= info.Length;
                        info.Delete();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Waveform cache evict failed: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Waveform cache enforce limit failed: {ex.Message}");
            }
        }

        private static string GetCachePath(string filePath)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(filePath));
            return Path.Combine(DefaultDirectory, Convert.ToHexString(hash) + FileExtension);
        }
    }
}
