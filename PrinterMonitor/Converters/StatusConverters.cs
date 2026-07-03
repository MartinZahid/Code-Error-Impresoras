using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace PrinterMonitor.Converters;

[ValueConversion(typeof(bool), typeof(Brush))]
public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool active = value is bool b && b;
        string? colorStr = parameter as string;
        return colorStr switch
        {
            "red" => active ? new SolidColorBrush(Color.FromRgb(0xD6, 0x30, 0x31)) : new SolidColorBrush(Color.FromRgb(0xB2, 0xBE, 0xC3)),
            "orange" => active ? new SolidColorBrush(Color.FromRgb(0xE1, 0x70, 0x55)) : new SolidColorBrush(Color.FromRgb(0xB2, 0xBE, 0xC3)),
            "yellow" => active ? new SolidColorBrush(Color.FromRgb(0xFD, 0xCB, 0x6E)) : new SolidColorBrush(Color.FromRgb(0xB2, 0xBE, 0xC3)),
            "green" => active ? new SolidColorBrush(Color.FromRgb(0x00, 0xB8, 0x94)) : new SolidColorBrush(Color.FromRgb(0xB2, 0xBE, 0xC3)),
            "blue" => active ? new SolidColorBrush(Color.FromRgb(0x09, 0x84, 0xE3)) : new SolidColorBrush(Color.FromRgb(0xB2, 0xBE, 0xC3)),
            _ => active ? new SolidColorBrush(Color.FromRgb(0xD6, 0x30, 0x31)) : new SolidColorBrush(Color.FromRgb(0xB2, 0xBE, 0xC3)),
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

[ValueConversion(typeof(bool), typeof(string))]
public class BoolToDotConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && b ? "\u25CF" : "\u25CB";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

[ValueConversion(typeof(bool), typeof(Visibility))]
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && b ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
