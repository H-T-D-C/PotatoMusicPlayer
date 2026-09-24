using System;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using PotatoMusicPlayer.Models;

namespace PotatoMusicPlayer.Services
{
    /// <summary>アプリケーションの配色を、設定値またはWindowsの配色設定から適用する。</summary>
    public static class ThemeService
    {
        public static bool IsLightMode { get; private set; }
        public static event EventHandler ThemeChanged;
        private const string DarkThemeUri = "Resources/Themes/DarkTheme.xaml";
        private const string LightThemeUri = "Resources/Themes/LightTheme.xaml";
        private static ThemeMode _currentMode = ThemeMode.System;
        private static bool _isWatchingSystemTheme;
        private static bool? _appliedLightTheme;

        public static void Apply(ThemeMode mode)
        {
            _currentMode = mode;
            bool watchSystemTheme = mode == ThemeMode.System;
            if (watchSystemTheme != _isWatchingSystemTheme)
            {
                if (watchSystemTheme)
                    SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
                else
                    SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
                _isWatchingSystemTheme = watchSystemTheme;
            }

            bool useLight = mode == ThemeMode.Light || (mode == ThemeMode.System && IsSystemLight());
            if (_appliedLightTheme == useLight)
                return;

            _appliedLightTheme = useLight;
            IsLightMode = useLight;
            var dictionaries = Application.Current?.Resources?.MergedDictionaries;
            if (dictionaries != null)
            {
                string targetUri = useLight ? LightThemeUri : DarkThemeUri;
                for (int index = dictionaries.Count - 1; index >= 0; index--)
                {
                    var source = dictionaries[index].Source?.OriginalString;
                    if (source != null &&
                        (source.EndsWith(DarkThemeUri, StringComparison.OrdinalIgnoreCase) ||
                         source.EndsWith(LightThemeUri, StringComparison.OrdinalIgnoreCase)))
                        dictionaries.RemoveAt(index);
                }

                dictionaries.Add(new ResourceDictionary { Source = new Uri(targetUri, UriKind.Relative) });
            }

            ThemeChanged?.Invoke(null, EventArgs.Empty);
        }

        private static void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.HasShutdownStarted)
                return;

            dispatcher.BeginInvoke(new Action(() =>
            {
                if (_currentMode == ThemeMode.System)
                    Apply(_currentMode);
            }), DispatcherPriority.ApplicationIdle);
        }

        private static bool IsSystemLight()
        {
            try
            {
                object value = Registry.GetValue(
                    @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                    "AppsUseLightTheme", 0);
                return value is int enabled && enabled != 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
