using System;
using System.Globalization;
using System.Windows.Data;

namespace PotatoMusicPlayer.Converters
{
    /// <summary>
    /// 0.0 ~ 1.0 の音量を "0%" ~ "100%" に変換
    /// </summary>
    [ValueConversion(typeof(float), typeof(string))]
    public class VolumeToPercentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is float volume)
            {
                return $"{(int)(volume * 100)}%";
            }

            return "0%";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str && str.EndsWith("%"))
            {
                if (float.TryParse(str.TrimEnd('%'), out var percent))
                {
                    return percent / 100.0f;
                }
            }

            return 0.8f;
        }
    }
}
