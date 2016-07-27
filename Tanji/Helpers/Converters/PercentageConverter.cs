using System;
using System.Globalization;

namespace Tanji.Helpers.Converters
{
    public class PercentageConverter : MultiConverter
    {
        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double max = ((values[1] as double?) ?? 100.0);
            double percent = ((values[0] as double?) ?? 100.0);

            return (percent * max) / 100.0;
        }
        public override object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}