using System;
using System.Globalization;
using System.Windows.Data;

namespace QuickStarted.Converters
{
    /// <summary>
    /// 将基于1的索引转换为基于0的数组索引
    /// </summary>
    public class IndexToArrayIndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int index)
            {
                return index - 1; // 将1-based索引转换为0-based索引
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int arrayIndex)
            {
                return arrayIndex + 1; // 将0-based索引转换为1-based索引
            }
            return 1;
        }
    }
}