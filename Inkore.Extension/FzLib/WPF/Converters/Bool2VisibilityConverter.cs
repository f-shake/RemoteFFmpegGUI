using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace iNKORE.Extension.FzLib.WPF.Converters
{
    /// <summary>
    /// 布尔转WPF可见性。默认true=>Visible, false=>Collapsed。若参数含'h'，则隐藏为Hidden；若参数含'i'，则反转true和false。
    /// </summary>
    public class Bool2VisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                b = ConverterHelper.GetInverseResult(b, parameter);
                return b ? Visibility.Visible : ConverterHelper.GetHiddenMode(parameter);
            }
            throw new ArgumentException("绑定值必须为Bool类型");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}