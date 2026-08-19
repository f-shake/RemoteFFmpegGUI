using System;
using System.Globalization;
using System.Windows.Data;

namespace SimpleFFmpegGUI.WPF.FzLib.WPF.Converters
{
    /// <summary>
    /// 通过参数将enum转换为string。
    /// 参数格式示例：Downloading:暂停下载;Paused:继续下载;Stop:开始下载;Pausing:正在暂停
    /// </summary>
    public class StringReplaceConverter : IValueConverter, IDefaultNullValue<string>
    {
        public string DefaultNullValue { get; set; } = "";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return DefaultNullValue;
            }
            if ( !(parameter is string))
            {
                throw new ArgumentNullException();
            }
            string[] paras = (parameter as string).Split(';');
            foreach (var item in paras)
            {
                string[] parts = item.Split(':');
                if (parts.Length < 2)
                {
                    throw new Exception("参数格式错误");
                }
                string key = parts[0];
                string str = parts.Length == 2 ? parts[1] : item.Substring(parts[0].Length + 1);
                if (value.ToString() == key)
                {
                    return str;
                }
            }
            throw new Exception("找不到指定的值");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}