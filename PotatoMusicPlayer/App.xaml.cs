using System;
using System.IO;
using System.Windows;
using PotatoMusicPlayer.Services;

namespace PotatoMusicPlayer
{
    public partial class App : Application
    {
        public static string StartupFilePath { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            StartupFilePath = FindStartupFile(e.Args);
            FileAssociationService.RegisterCurrentUserAssociations();
            ThemeService.Apply(new SettingsService().GetSettings().Theme);

            // 未処理例外のハンドリング（アプリが落ちる前にログを残す）
            DispatcherUnhandledException += (s, args) =>
            {
                MessageBox.Show(
                    $"予期しないエラーが発生しました:\n{args.Exception.Message}",
                    "Potato Music Player - エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                args.Handled = true;
            };
        }

        private static string FindStartupFile(string[] args)
        {
            if (args == null)
                return string.Empty;

            foreach (string argument in args)
            {
                if (!string.IsNullOrWhiteSpace(argument) &&
                    File.Exists(argument) && FileService.IsSupportedFormat(argument))
                    return Path.GetFullPath(argument);
            }

            return string.Empty;
        }
    }
}
