using System;
using System.Windows;
using PotatoMusicPlayer.Services;

namespace PotatoMusicPlayer
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
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
    }
}
