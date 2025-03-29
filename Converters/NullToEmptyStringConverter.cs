using System;
using System.Globalization;
using System.Windows.Data;

namespace PlanifKanban.Converters
{
    public class NullToEmptyStringConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] == null || values[1] == null)
                return string.Empty;

            string description = values[0].ToString();
            bool showDescription = (bool)values[1];

            if (!showDescription)
                return string.Empty;

            return description ?? string.Empty;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}