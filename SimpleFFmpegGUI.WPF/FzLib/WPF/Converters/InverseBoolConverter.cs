using System;
using System.Globalization;
using System.Windows.Data;

namespace SimpleFFmpegGUI.WPF.FzLib.WPF.Converters
{
    /// <summary>
    /// 反转bool
    /// </summary>
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return !b;
            }
            throw new ArgumentException(nameof(value));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}