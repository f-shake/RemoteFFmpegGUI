using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SimpleFFmpegGUI.WPF.FzLib.WPF.Converters
{
    /// <summary>
    /// 绑定值和参数相等，则Visible/true，否则Collapsed。参数以"#h"结尾，则为Hidden；#i结尾，则反转。。
    /// </summary>
    public class EqualConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter==null)
            {
                throw new ArgumentNullException();
            }
            bool result = false;
            if (parameter.Equals(value) || value != null && value.ToString().Equals(ConverterHelper.RemoveEndFlags(parameter.ToString())))
            {
                result = true;
            }
            result = ConverterHelper.GetEndInverseResult(result, parameter);
            if (targetType == typeof(bool) || targetType == typeof(bool?))
            {
                return result;
            }
            if (targetType == typeof(Visibility))
            {
                return result ? Visibility.Visible : ConverterHelper.GetEndHiddenMode(parameter);
            }
            throw new ArgumentException(nameof(targetType));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}