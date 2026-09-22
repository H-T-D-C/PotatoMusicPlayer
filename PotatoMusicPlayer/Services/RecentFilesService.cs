using System.IO;

namespace PotatoMusicPlayer.Services;

/// <summary>
/// 最近使ったファイルを管理するサービス
/// </summary>
public class RecentFilesService
{
    private readonly List<string> _recentFiles;
    private readonly int _maxCount;

    public event EventHandler? RecentFilesChanged;

    public RecentFilesService(int maxCount = 10)
    {
        _maxCount = maxCount;
        _recentFiles = new List<string>();
    }

    /// <summary>
    /// 最近使ったファイルを初期化
    /// </summary>
    public void Initialize(List<string> files)
    {
        _recentFiles.Clear();
        _recentFiles.AddRange(files.Take(_maxCount));
    }

    /// <summary>
    /// ファイルを最近使ったリストに追加
    /// </summary>
    public void AddFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        // 既に存在していれば削除（上に移動させるため）
        _recentFiles.Remove(filePath);

        // リストの最初に追加
        _recentFiles.Insert(0, filePath);

        // 超過分を削除
        while (_recentFiles.Count > _maxCount)
        {
            _recentFiles.RemoveAt(_recentFiles.Count - 1);
        }

        RecentFilesChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 最近使ったファイルを取得
    /// </summary>
    public List<string> GetRecentFiles()
    {
        return new List<string>(_recentFiles);
    }

    /// <summary>
    /// 最近使ったファイルをクリア
    /// </summary>
    public void Clear()
    {
        _recentFiles.Clear();
        RecentFilesChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// ファイルが存在するかチェック
    /// </summary>
    public bool FileExists(string filePath)
    {
        return File.Exists(filePath);
    }

    /// <summary>
    /// 存在しないファイルをリストから削除
    /// </summary>
    public void RemoveNonExistentFiles()
    {
        var toRemove = _recentFiles.Where(f => !File.Exists(f)).ToList();
        foreach (var file in toRemove)
        {
            _recentFiles.Remove(file);
        }

        if (toRemove.Count > 0)
            RecentFilesChanged?.Invoke(this, EventArgs.Empty);
    }
}
