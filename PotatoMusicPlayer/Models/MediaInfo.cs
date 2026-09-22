namespace PotatoMusicPlayer.Models;

/// <summary>
/// メディアファイルのメタデータ情報モデル
/// </summary>
public class MediaInfo
{
    /// <summary>
    /// ファイルパス
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 曲名
    /// </summary>
    public string Title { get; set; } = "Unknown Title";

    /// <summary>
    /// アーティスト名
    /// </summary>
    public string Artist { get; set; } = "Unknown Artist";

    /// <summary>
    /// アルバム名
    /// </summary>
    public string Album { get; set; } = "Unknown Album";

    /// <summary>
    /// 再生時間
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// ビットレート（bps）
    /// </summary>
    public int BitRate { get; set; }

    /// <summary>
    /// サンプリングレート（Hz）
    /// </summary>
    public int SampleRate { get; set; }

    /// <summary>
    /// チャンネル数
    /// </summary>
    public int Channels { get; set; }

    public override string ToString()
    {
        return $"{Title} - {Artist}";
    }
}
