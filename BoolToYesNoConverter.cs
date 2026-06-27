using System;
using System.Globalization;
using System.Windows.Data;

namespace AnimalShelterAI.Converters
{
    public class BoolToYesNoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return boolValue ? "Да" : "Нет";
            return "Нет";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string strValue)
                return strValue == "Да";
            return false;
        }
    }
}