using System;
using System.Globalization;

namespace Tanji.Helpers.Converters
{
    public class BooleanOrConverter : MultiConverter
    {
        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2)
                return false;

            var leftOr = (values[0] as bool?);
            var rightOr = (values[1] as bool?);

            return ((leftOr ?? false)
                || (rightOr ?? false));
        }

        public override object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}