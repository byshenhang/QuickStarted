using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace QuickStarted.Converters
{
    /// <summary>
    /// Null值到ImageSource转换器
    /// 当值为null或空字符串时返回null，否则尝试创建ImageSource
    /// </summary>
    public class NullToImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || (value is string str && string.IsNullOrEmpty(str)))
            {
                return null;
            }

            try
            {
                if (value is string path)
                {
                    return new System.Windows.Media.Imaging.BitmapImage(new Uri(path, UriKind.RelativeOrAbsolute));
                }
                return value;
            }
            catch
            {
                // 如果图片加载失败，返回null
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}