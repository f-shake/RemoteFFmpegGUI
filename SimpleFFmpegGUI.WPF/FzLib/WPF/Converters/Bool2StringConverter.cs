using System;
using System.Globalization;
using System.Windows.Data;

namespace SimpleFFmpegGUI.WPF.FzLib.WPF.Converters
{
    /// <summary>
    /// 布尔类型转字符串，格式：true:false。包含冒号：true:fa\:lse
    /// </summary>
    public class Bool2StringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ( !(parameter is string))
            {
                throw new ArgumentNullException(nameof(parameter));
            }
            string[] parts = (parameter as string).Replace("\\:", "{colon}").Split(':');
            if (parts.Length != 2)
            {
                throw new Exception("参数格式错误");
            }

            return ((bool)value ? parts[0] : parts[1]).Replace("{colon}", ":");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}