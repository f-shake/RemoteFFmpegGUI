using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SimpleFFmpegGUI.WPF.FzLib.WPF.Converters
{
    /// <summary>
    /// 多布尔值取“与”后转 WPF 可见性：全部为 true => Visible，否则 => Collapsed。
    /// 用于把多个开启条件组合成一个按钮的可见性（配合 InverseBoolConverter 实现取反）。
    /// </summary>
    public class MultiAndBooleanToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            foreach (var value in values)
            {
                if (value is not bool b || !b)
                {
                    return Visibility.Collapsed;
                }
            }
            return Visibility.Visible;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
