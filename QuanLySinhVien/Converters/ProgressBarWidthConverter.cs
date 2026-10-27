using System.Globalization;
using System.Windows.Data;

namespace QuanLySinhVien.Converters;

public sealed class ProgressBarWidthConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 3 ||
            values[0] is not double width ||
            values[1] is not double value ||
            values[2] is not double maximum ||
            maximum <= 0)
        {
            return 0d;
        }

        return Math.Max(0, Math.Min(width, width * value / maximum));
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
