using System;
using System.Globalization;
using System.Windows.Data;

namespace PotatoMusicPlayer.Converters
{
    /// <summary>
    /// TimeSpan を "HH:MM:SS" または "MM:SS" の文字列に変換
    /// </summary>
    [ValueConversion(typeof(TimeSpan), typeof(string))]
    public class TimeSpanToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimeSpan ts)
            {
                if (ts.Hours > 0)
                    return ts.ToString(@"hh\:mm\:ss");
                else
                    return ts.ToString(@"mm\:ss");
            }

            return "00:00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str && TimeSpan.TryParse(str, out var result))
                return result;

            return TimeSpan.Zero;
        }
    }
}
