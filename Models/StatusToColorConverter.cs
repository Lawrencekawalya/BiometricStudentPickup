using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace BiometricStudentPickup.Models
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status.ToLower() switch
                {
                    "present" => new SolidColorBrush(Color.FromRgb(34, 197, 94)), // Green
                    "late" => new SolidColorBrush(Color.FromRgb(245, 158, 11)),   // Amber
                    "absent" => new SolidColorBrush(Color.FromRgb(239, 68, 68)),  // Red
                    "excused" => new SolidColorBrush(Color.FromRgb(59, 130, 246)), // Blue
                    _ => new SolidColorBrush(Color.FromRgb(107, 114, 128))        // Gray
                };
            }
            return new SolidColorBrush(Color.FromRgb(107, 114, 128)); // Default gray
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}