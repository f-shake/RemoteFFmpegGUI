using System;
using System.Collections;
using System.Globalization;
using System.Windows.Data;
using System.Linq;
using System.Windows;

namespace SimpleFFmpegGUI.WPF.FzLib.WPF.Converters
{
    /// <summary>
    /// 集合的数量>0则返回true/Visiable。支持参数i反转，参数h使用Hidden代替Collapse。
    /// </summary>
    public class CountMoreThanZeroConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool result = false;
            if (value is ICollection c)
            {
                result = c.Count > 0;
            }
            else if (value is IEnumerable e)
            {
                result = e.Cast<object>().Any();
            }
            result = ConverterHelper.GetInverseResult(result, parameter);

            if (targetType == typeof(bool) || targetType == typeof(bool?))
            {
                return result;
            }
            if (targetType == typeof(Visibility))
            {
                return result ? Visibility.Visible : ConverterHelper.GetHiddenMode(parameter);
            }
            throw new ArgumentException(nameof(targetType));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}