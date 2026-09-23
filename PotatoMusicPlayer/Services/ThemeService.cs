using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using PotatoMusicPlayer.Models;

namespace PotatoMusicPlayer.Services
{
    /// <summary>アプリケーションの配色を、設定値またはWindowsの配色設定から適用する。</summary>
    public static class ThemeService
    {
        public static bool IsLightMode { get; private set; }
        public static event EventHandler ThemeChanged;

        public static void Apply(ThemeMode mode)
        {
            bool useLight = mode == ThemeMode.Light || (mode == ThemeMode.System && IsSystemLight());
            IsLightMode = useLight;
            SetBrush("BackgroundDarkBrush", useLight ? "#F3F3F3" : "#1E1E1E");
            SetBrush("BackgroundMediumBrush", useLight ? "#E7E7E7" : "#2D2D2D");
            SetBrush("BackgroundLightBrush", useLight ? "#D2D2D2" : "#3C3C3C");
            SetBrush("BorderBrush", useLight ? "#BDBDBD" : "#4A4A4A");
            SetBrush("TextLightBrush", useLight ? "#252525" : "#DCDCDC");
            SetBrush("TextDimBrush", useLight ? "#4B4B4B" : "#969696");
            SetBrush("WaveformBrush", useLight ? "#397AB3" : "#78B4FF");
            SetBrush("ControlBackgroundBrush", useLight ? "#F2F2F2" : "#3C3C3C");
            SetBrush("ControlBorderBrush", useLight ? "#A8A8A8" : "#666666");
            SetBrush("MenuHoverBrush", useLight ? "#D6DCE3" : "#3C3C3C");
            SetBrush("WaveformBackgroundBrush", useLight ? "#E1E1E1" : "#252525");
            ThemeChanged?.Invoke(null, EventArgs.Empty);
        }

        private static void SetBrush(string key, string color)
        {
            var resources = Application.Current?.Resources;
            SolidColorBrush brush = resources?[key] as SolidColorBrush;
            if (brush == null && resources != null)
            {
                foreach (var dictionary in resources.MergedDictionaries)
                {
                    if (dictionary[key] is SolidColorBrush mergedBrush)
                    {
                        brush = mergedBrush;
                        break;
                    }
                }
            }

            if (brush == null)
                return;

            var newColor = (Color)ColorConverter.ConvertFromString(color);
            if (brush.IsFrozen)
            {
                // XAML由来のFreezableは最適化のため凍結されている場合がある。
                // その場合は変更可能な複製をリソースへ戻す。
                var replacement = brush.Clone();
                replacement.Color = newColor;
                if (resources != null)
                    resources[key] = replacement;
            }
            else
            {
                brush.Color = newColor;
            }
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
