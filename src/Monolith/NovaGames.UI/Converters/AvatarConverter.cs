using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace NovaGames.UI.Converters
{
    public class AvatarConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string url = value as string;
            if (string.IsNullOrEmpty(url)) return null;
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();

                if (url.StartsWith("/images/"))
                {
                    bitmap.UriSource = new Uri($"https://localhost:7232{url}", UriKind.Absolute);
                    bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                }
                else
                {
                    byte[] imageBytes = System.Convert.FromBase64String(url);
                    bitmap.StreamSource = new MemoryStream(imageBytes);
                }

                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}