using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace SimpleFFmpegGUI.WPF.FzLib.WPF.Converters
{
    /// <summary>
    /// TimeSpan和String互转，默认支持的格式：12:34:56，12s，34m，56h
    /// </summary>
    public class TimeSpanConverter : IValueConverter
    {
        public string Format { get; set; } = "hh\\:mm\\:ss";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }
            if (value is TimeSpan ts)
            {
                return ts.ToString(Format);
            }
            throw new Exception("绑定源必须为TimeSpan");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }
            if (value is string str)
            {
                str = str.Trim()
                    .ToLower()
                    .Replace(";", ":")
                    .Replace("；", ":")
                    .Replace('：', ':');

                if (str.Count(p => p == ':') == 1)
                {
                    str = "0:" + str;
                }
                if (TimeSpan.TryParse(str, out TimeSpan t))
                {
                    return t;
                }
                if (TimeSpan.TryParseExact(str, Format, CultureInfo.CurrentCulture, out TimeSpan t2))
                {
                    return t2;
                }
                if (double.TryParse(str.TrimEnd('s'), out double s))
                {
                    return TimeSpan.FromSeconds(s);
                }
                if (str.EndsWith('m') && double.TryParse(str.TrimEnd('m'), out double m))
                {
                    return TimeSpan.FromMinutes(m);
                }
                if (str.EndsWith('h') && double.TryParse(str.TrimEnd('h'), out double h))
                {
                    return TimeSpan.FromHours(h);
                }
                return null;
                //throw new Exception("转换失败");
            }
            throw new Exception("绑定目标必须为String");
        }
    }
}