using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using PotatoMusicPlayer.Models;

namespace PotatoMusicPlayer.Services
{
    /// <summary>
    /// ファイル操作に関するユーティリティ
    /// </summary>
    public static class FileService
    {
        private static readonly string[] SupportedFormats = { ".mp3", ".flac", ".wav", ".ogg" };

        /// <summary>
        /// ファイルが対応フォーマットかチェック
        /// </summary>
        public static bool IsSupportedFormat(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            string extension = Path.GetExtension(filePath).ToLower();
            return Array.Exists(SupportedFormats, fmt => fmt == extension);
        }

        /// <summary>
        /// ファイルを開くダイアログ用フィルター文字列を取得
        /// </summary>
        public static string GetFileDialogFilter()
        {
            return "Audio Files|*.mp3;*.flac;*.wav;*.ogg|All Files|*.*";
        }

        /// <summary>
        /// フォルダまたはファイルをエクスプローラーで開く。
        /// - フォルダパスが渡された場合はそのフォルダを開く。
        /// - ファイルパスが渡された場合はそのファイルを選択表示する。
        /// </summary>
        public static void OpenFolderInExplorer(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                    return;

                // ファイルが存在する場合は /select, を使ってファイルを選択
                if (File.Exists(path))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"/select,\"{path}\"",
                        UseShellExecute = true
                    });
                    return;
                }

                // フォルダが存在する場合はそのフォルダを開く
                if (Directory.Exists(path))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"\"{path}\"",
                        UseShellExecute = true
                    });
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to open folder: {ex.Message}");
            }
        }

        /// <summary>
        /// フォルダをコマンドプロンプトで開く
        /// </summary>
        public static void OpenFolderInTerminal(string folderPath)
        {
            try
            {
                if (!Directory.Exists(folderPath))
                    return;

                // Windows 11 Terminal（ある場合）を優先的に使用
                var processInfo = new ProcessStartInfo
                {
                    FileName = "wt.exe",
                    Arguments = $"-w new-tab -d \"{folderPath}\"",
                    UseShellExecute = true
                };

                try
                {
                    Process.Start(processInfo);
                }
                catch
                {
                    // Windows Terminal がない場合は cmd を使用
                    processInfo.FileName = "cmd.exe";
                    processInfo.Arguments = $"/k cd /d \"{folderPath}\"";
                    Process.Start(processInfo);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to open terminal: {ex.Message}");
            }
        }

        /// <summary>
        /// ファイルパスから親フォルダを取得
        /// </summary>
        public static string GetParentDirectory(string filePath)
        {
            return Path.GetDirectoryName(filePath) ?? "";
        }

        /// <summary>
        /// ファイルが存在するか確認
        /// </summary>
        public static bool FileExists(string filePath)
        {
            return !string.IsNullOrEmpty(filePath) && File.Exists(filePath);
        }

        /// <summary>
        /// ファイルサイズを取得（人間が読める形式）
        /// </summary>
        public static string GetFileSizeDisplay(string filePath)
        {
            try
            {
                var info = new FileInfo(filePath);
                return FormatBytes(info.Length);
            }
            catch
            {
                return "Unknown";
            }
        }

        /// <summary>
        /// バイト数を人間が読める形式に変換
        /// </summary>
        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }
    }
}
