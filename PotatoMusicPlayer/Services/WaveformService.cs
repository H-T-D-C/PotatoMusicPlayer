using System;
using System.Threading.Tasks;
using NAudio.Wave;

namespace PotatoMusicPlayer.Services
{
    /// <summary>
    /// 音声ファイルから波形表示用の振幅データを抽出するサービス。
    ///
    /// 注意: 再生エンジンは LibVLCSharp を使用しているが、
    /// 波形の事前解析（デコードして振幅を取り出す処理）には NAudio を使用する。
    /// これは LibVLC から生のPCMサンプルを取り出すには追加のコールバック実装が必要で
    /// 複雑になるため、実績のある NAudio のデコーダーを流用する方針とした。
    ///
    /// NAudio の AudioFileReader は WAV / MP3 を標準サポート。
    /// FLAC / OGG は Windows Media Foundation 経由のデコード（MediaFoundationReader）を
    /// フォールバックとして使用するが、環境によっては非対応の場合がある。
    /// </summary>
    public class WaveformService
    {
        /// <summary>
        /// 波形データ生成の進捗通知（0.0 ~ 1.0）
        /// </summary>
        public event EventHandler<double> ProgressChanged;

        /// <summary>
        /// 指定したバー数の振幅データ（0.0 ~ 1.0の正規化された値）を非同期生成する。
        /// 動画編集ソフト風の「音量レベルの棒グラフ」表示を想定し、
        /// 各バーはその区間内のピーク振幅（絶対値の最大）を表す。
        /// </summary>
        /// <param name="filePath">音声ファイルパス</param>
        /// <param name="barCount">生成するバーの本数（UIの幅に応じて可変）</param>
        public Task<float[]> GenerateWaveformAsync(string filePath, int barCount)
        {
            return Task.Run(() => GenerateWaveform(filePath, barCount));
        }

        private float[] GenerateWaveform(string filePath, int barCount)
        {
            var result = new float[barCount];

            try
            {
                using (var reader = CreateReader(filePath))
                {
                    if (reader == null)
                        return result;

                    var sampleProvider = reader.ToSampleProvider();
                    long totalSamples = reader.Length / (reader.WaveFormat.BitsPerSample / 8) / reader.WaveFormat.Channels;

                    if (totalSamples <= 0)
                        return result;

                    long samplesPerBar = Math.Max(1, totalSamples / barCount);
                    int channels = reader.WaveFormat.Channels;

                    // 読み込みバッファ（1バーぶんのサンプル×チャンネル数）
                    int bufferSize = (int)Math.Min(samplesPerBar * channels, 1_000_000);
                    var buffer = new float[bufferSize];

                    for (int bar = 0; bar < barCount; bar++)
                    {
                        float peak = 0f;
                        long samplesNeeded = samplesPerBar * channels;
                        long samplesRead = 0;

                        while (samplesRead < samplesNeeded)
                        {
                            int toRead = (int)Math.Min(bufferSize, samplesNeeded - samplesRead);
                            int read = sampleProvider.Read(buffer, 0, toRead);
                            if (read == 0)
                                break;  // ファイル終端

                            for (int i = 0; i < read; i++)
                            {
                                float abs = Math.Abs(buffer[i]);
                                if (abs > peak) peak = abs;
                            }

                            samplesRead += read;
                        }

                        result[bar] = Math.Clamp(peak, 0f, 1f);

                        if (bar % 20 == 0)
                            ProgressChanged?.Invoke(this, (double)bar / barCount);
                    }
                }
            }
            catch (Exception)
            {
                // 波形生成に失敗しても再生自体には影響させない（空データを返すのみ）
                return new float[barCount];
            }

            ProgressChanged?.Invoke(this, 1.0);
            return result;
        }

        /// <summary>
        /// ファイル形式に応じた WaveStream を生成する。
        /// MP3/WAV は AudioFileReader、それ以外は MediaFoundationReader をフォールバックとして使用。
        /// </summary>
        private WaveStream CreateReader(string filePath)
        {
            string ext = System.IO.Path.GetExtension(filePath).ToLowerInvariant();

            try
            {
                switch (ext)
                {
                    case ".wav":
                    case ".mp3":
                        return new AudioFileReader(filePath);
                    case ".flac":
                    case ".ogg":
                    default:
                        // Windows Media Foundation 経由のデコード（環境依存）
                        return new MediaFoundationReader(filePath);
                }
            }
            catch
            {
                // AudioFileReader が失敗した場合、MediaFoundationReader にフォールバック
                try
                {
                    return new MediaFoundationReader(filePath);
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}
