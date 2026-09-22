using System.IO;
using TagLib;

namespace PotatoMusicPlayer.Core;

/// <summary>
/// TagLibSharp を使用したメタデータ抽出クラス
/// </summary>
public class MetadataReader
{
    private readonly string[] _supportedFormats = { ".mp3", ".flac", ".wav", ".ogg", ".wma" };

    public MetadataReader()
    {
    }

    /// <summary>
    /// ファイルからメタデータを読み込む
    /// </summary>
    public Models.MediaInfo ReadMetadata(string filePath)
    {
        var info = new Models.MediaInfo { FilePath = filePath };

        try
        {
            if (!System.IO.File.Exists(filePath))
                return info;

            var file = TagLib.File.Create(filePath);

            // 楽曲情報の抽出
            info.Title = !string.IsNullOrWhiteSpace(file.Tag.Title)
                ? file.Tag.Title
                : Path.GetFileNameWithoutExtension(filePath);

            info.Artist = file.Tag.FirstPerformer ?? "Unknown Artist";
            info.Album = file.Tag.Album ?? "Unknown Album";

            // 再生時間
            info.Duration = file.Properties.Duration;

            // ビットレート（bps）
            info.BitRate = file.Properties.AudioBitrate;

            // サンプリングレート
            info.SampleRate = file.Properties.AudioSampleRate;

            // チャンネル数
            info.Channels = file.Properties.AudioChannels;

            return info;
        }
        catch (Exception ex)
        {
            // メタデータ読み込み失敗時は、ファイル名から情報を取得
            info.Title = Path.GetFileNameWithoutExtension(filePath);
            info.Artist = "Unknown";
            return info;
        }
    }

    /// <summary>
    /// ファイルが対応フォーマットかチェック
    /// </summary>
    public bool IsSupportedFormat(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLower();
        return _supportedFormats.Any(f => f.Equals(extension, StringComparison.OrdinalIgnoreCase));
    }
}
