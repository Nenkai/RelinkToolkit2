using Avalonia.Data.Converters;
using Avalonia.Styling;

using System;
using System.Globalization;

namespace RelinkToolkit2.Converters;

public class ObjectToStyleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
            return string.Empty;

        return value.ToString()!;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
