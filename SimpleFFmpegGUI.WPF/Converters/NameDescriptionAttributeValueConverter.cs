using SimpleFFmpegGUI.Attributes;
using System;
using System.Globalization;
using System.Windows.Data;

namespace SimpleFFmpegGUI.WPF.Converters
{
    public class NameDescriptionAttributeValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return AttributeHelper.GetAttributeValue<NameDescriptionAttribute, string>(value, p => p.Name);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }
}