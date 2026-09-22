using System.IO;
using Newtonsoft.Json;
using PotatoMusicPlayer.Models;

namespace PotatoMusicPlayer.Services;

/// <summary>
/// JSON ファイルからアプリケーション設定を読み込み・保存するサービス
/// </summary>
public class ConfigService
{
    private readonly string _configDirectory;
    private readonly string _configFilePath;
    private AppSettings _currentSettings;

    public ConfigService()
    {
        _configDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PotatoMusicPlayer"
        );

        _configFilePath = Path.Combine(_configDirectory, "settings.json");
        _currentSettings = new AppSettings();
    }

    /// <summary>
    /// 設定を読み込む（ファイルが存在しない場合はデフォルト設定を作成）
    /// </summary>
    public AppSettings LoadSettings()
    {
        try
        {
            if (!Directory.Exists(_configDirectory))
                Directory.CreateDirectory(_configDirectory);

            if (File.Exists(_configFilePath))
            {
                var json = File.ReadAllText(_configFilePath);
                _currentSettings = JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
            }
            else
            {
                _currentSettings = new AppSettings();
                SaveSettings();
            }

            return _currentSettings;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"設定の読み込みに失敗: {ex.Message}");
            return new AppSettings();
        }
    }

    /// <summary>
    /// 設定を保存
    /// </summary>
    public void SaveSettings()
    {
        try
        {
            if (!Directory.Exists(_configDirectory))
                Directory.CreateDirectory(_configDirectory);

            var json = JsonConvert.SerializeObject(_currentSettings, Formatting.Indented);
            File.WriteAllText(_configFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"設定の保存に失敗: {ex.Message}");
        }
    }

    /// <summary>
    /// 現在の設定を取得
    /// </summary>
    public AppSettings GetSettings()
    {
        return _currentSettings;
    }

    /// <summary>
    /// 設定を更新
    /// </summary>
    public void UpdateSettings(AppSettings settings)
    {
        _currentSettings = settings;
    }

    /// <summary>
    /// 設定をリセット（デフォルト値に戻す）
    /// </summary>
    public void ResetToDefaults()
    {
        _currentSettings = new AppSettings();
        SaveSettings();
    }
}
