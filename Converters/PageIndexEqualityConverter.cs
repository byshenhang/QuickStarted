using System;
using System.Globalization;
using System.Windows.Data;

namespace QuickStarted.Converters
{
    /// <summary>
    /// 页面索引相等性转换器，用于判断当前页面是否为选中页面
    /// </summary>
    public class PageIndexEqualityConverter : IMultiValueConverter
    {
        /// <summary>
        /// 转换方法，比较页面索引和当前页面索引是否相等
        /// </summary>
        /// <param name="values">值数组，[0]为页面索引(1-based)，[1]为当前页面索引(0-based)</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">参数</param>
        /// <param name="culture">文化信息</param>
        /// <returns>如果相等返回true，否则返回false</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is int pageIndex && values[1] is int currentPageIndex)
            {
                // 将1-based的页面索引转换为0-based进行比较
                return (pageIndex - 1) == currentPageIndex;
            }
            return false;
        }

        /// <summary>
        /// 反向转换方法（未实现）
        /// </summary>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}