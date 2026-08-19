using SimpleFFmpegGUI.WPF.FzLib;
using System;
using System.Globalization;
using System.Windows.Data;

namespace SimpleFFmpegGUI.WPF.Converters
{
    public class Bitrate2StringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            long num = System.Convert.ToInt64(value);
            string str = NumberConverter.ByteToFitString(num, 2, " bps", " Kbps", " Mbps", " Gbps", " Tbps");
            return str;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}