using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace NovaGames.UI.Converters
{
    public class HexToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string hex && !string.IsNullOrEmpty(hex))
            {
                try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); }
                catch { return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00ADEF")); }
            }
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00ADEF"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}