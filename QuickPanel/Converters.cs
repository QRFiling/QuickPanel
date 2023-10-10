using System;
using System.Globalization;
using System.Windows.Data;

namespace QuickPanel
{
    public class StringDateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            DateTime.TryParse(value.ToString(), out DateTime result);

            if (result == DateTime.MinValue) return null;
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
