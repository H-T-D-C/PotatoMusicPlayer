using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PotatoMusicPlayer
{
    public partial class MainWindow : Window
    {
        // Recent files menu handling
        private void UpdateRecentFilesMenu()
        {
            try
            {
                RecentFilesMenuItem.Items.Clear();

                var recent = _viewModel?.Settings?.RecentFiles;
                if (recent == null || recent.Count == 0)
                {
                    var none = new MenuItem { Header = "(なし)", IsEnabled = false };
                    RecentFilesMenuItem.Items.Add(none);
                    return;
                }

                int maxToShow = Math.Min(10, recent.Count);
                for (int i = 0; i < maxToShow; i++)
                {
                    var path = recent[i];
                    var mi = new MenuItem { Header = System.IO.Path.GetFileName(path), ToolTip = path, Tag = path };
                    mi.Click += RecentFileMenuItem_Click;
                    RecentFilesMenuItem.Items.Add(mi);
                }

                RecentFilesMenuItem.Items.Add(new Separator());
                var clear = new MenuItem { Header = "最近の履歴をクリア" };
                clear.Click += ClearRecentFiles_Click;
                RecentFilesMenuItem.Items.Add(clear);
            }
            catch (Exception) { /* ignore UI update failures */ }
        }

        private async void RecentFileMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.Tag is string path)
            {
                await _viewModel.LoadAndPlayFileAsync(path);
                UpdateRecentFilesMenu();
            }
        }

        private void ClearRecentFiles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settingsService = new PotatoMusicPlayer.Services.SettingsService();
                settingsService.ClearRecentFiles();

                // Also update in-memory settings accessed by viewmodel
                var s = _viewModel.Settings;
                s.RecentFiles.Clear();
                settingsService.SaveSettings(s);

                UpdateRecentFilesMenu();
            }
            catch (Exception) { }
        }
    }
}
