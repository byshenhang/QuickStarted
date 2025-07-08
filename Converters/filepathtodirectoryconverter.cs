using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace QuickStarted.Converters
{
    /// <summary>
    /// 文件路径到目录路径转换器
    /// </summary>
    public class FilePathToDirectoryConverter : IValueConverter
    {
        /// <summary>
        /// 转换方法
        /// </summary>
        /// <param name="value">输入值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">参数</param>
        /// <param name="culture">文化信息</param>
        /// <returns>转换后的值</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string filePath && !string.IsNullOrEmpty(filePath))
            {
                try
                {
                    var directory = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        return new Uri(directory + Path.DirectorySeparatorChar);
                    }
                }
                catch
                {
                    // 如果路径无效，返回null
                }
            }
            return null;
        }

        /// <summary>
        /// 反向转换方法
        /// </summary>
        /// <param name="value">输入值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">参数</param>
        /// <param name="culture">文化信息</param>
        /// <returns>转换后的值</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}