using System;
using System.Globalization;
using System.Windows.Data;

namespace PlanifKanban.Converters
{
    public class RowIndexToPositionConverter : IValueConverter
    {
        private const int RowHeight = 55; // Hauteur d'une ligne en pixels (augmentée)
        private const int RowSpacing = 10;  // Espacement entre les lignes (augmenté)

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int rowIndex)
            {
                return rowIndex * (RowHeight + RowSpacing);
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}