using System;
using System.Globalization;
using System.Windows.Data;
// Add this converter class
namespace BiometricStudentPickup.Models
{
    public class EventTypeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string eventType)
            {
                return eventType switch
                {
                    "GuardianScan" => "#3498DB",   // Blue
                    "PickupComplete" => "#27AE60", // Green
                    "PickupTimeout" => "#E74C3C",  // Red
                    "Requested" => "#F39C12",      // Orange
                    _ => "#95A5A6"                 // Gray (default)
                };
            }
            return "#95A5A6";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}