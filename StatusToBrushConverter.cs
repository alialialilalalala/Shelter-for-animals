using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace AnimalShelterAI.Converters
{
    public class StatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status switch
                {
                    "Pending" => Brushes.Orange,
                    "Approved" => Brushes.Green,
                    "Rejected" => Brushes.Red,
                    "Completed" => Brushes.Blue,
                    "Quarantine" => Brushes.Orange,
                    "Available" => Brushes.Green,
                    "Reserved" => Brushes.Blue,
                    "Adopted" => Brushes.Purple,
                    "Treatment" => Brushes.Red,
                    "Healthy" => Brushes.Green,
                    "Sick" => Brushes.Red,
                    "Recovering" => Brushes.Yellow,
                    "Chronic" => Brushes.Brown,
                    _ => Brushes.Gray
                };
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}