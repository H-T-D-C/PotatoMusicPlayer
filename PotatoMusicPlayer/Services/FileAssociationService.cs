using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace PotatoMusicPlayer.Services
{
    /// <summary>現在のユーザーに音声ファイルの「プログラムから開く」情報を登録する。</summary>
    public static class FileAssociationService
    {
        private const string ProgId = "PotatoMusicPlayer.Audio";
        private static readonly string[] Extensions = { ".mp3", ".flac", ".wav", ".ogg" };

        public static void RegisterCurrentUserAssociations()
        {
            try
            {
                string executablePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrWhiteSpace(executablePath) ||
                    !executablePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                    Path.GetFileName(executablePath).Equals("dotnet.exe", StringComparison.OrdinalIgnoreCase))
                    return;

                string applicationName = Path.GetFileName(executablePath);
                string command = $"\"{executablePath}\" \"%1\"";

                using (var applicationKey = Registry.CurrentUser.CreateSubKey(
                    $"Software\\Classes\\Applications\\{applicationName}"))
                {
                    applicationKey?.SetValue("FriendlyAppName", "Potato Music Player");
                    using var supportedTypes = applicationKey?.CreateSubKey("SupportedTypes");
                    foreach (string extension in Extensions)
                        supportedTypes?.SetValue(extension, string.Empty);

                    using var openCommand = applicationKey?.CreateSubKey("shell\\open\\command");
                    openCommand?.SetValue(string.Empty, command);
                }

                using (var progIdKey = Registry.CurrentUser.CreateSubKey($"Software\\Classes\\{ProgId}"))
                {
                    progIdKey?.SetValue(string.Empty, "Potato Music Player Audio");
                    using var iconKey = progIdKey?.CreateSubKey("DefaultIcon");
                    iconKey?.SetValue(string.Empty, $"\"{executablePath}\",0");
                    using var openCommand = progIdKey?.CreateSubKey("shell\\open\\command");
                    openCommand?.SetValue(string.Empty, command);
                }

                foreach (string extension in Extensions)
                {
                    using var openWith = Registry.CurrentUser.CreateSubKey(
                        $"Software\\Classes\\{extension}\\OpenWithProgids");
                    openWith?.SetValue(ProgId, string.Empty);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to register file associations: {ex.Message}");
            }
        }
    }
}
