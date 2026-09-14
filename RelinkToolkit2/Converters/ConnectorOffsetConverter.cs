using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;

using System;
using System.Globalization;
using System.Windows;

namespace RelinkToolkit2.Converters;

public class ConnectorOffsetConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        double offset = System.Convert.ToDouble(parameter);
        if (value is Size s)
        {
            return new Size((s.Width + offset) / 2, (s.Height + offset) / 2);
        }

        return new Size(offset / 2, offset / 2);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        double offset = System.Convert.ToDouble(parameter);
        if (value is Size s)
        {
            return new Size((s.Width + offset) / 2, (s.Height + offset) / 2);
        }

        return new Size(offset / 2, offset / 2);
    }
}


/// <summary>
/// Converts a bool to a GridLength: true -> the length in ConverterParameter (or Auto), false -> 0.
/// </summary>
public class BoolToGridLengthConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isVisible = value is bool b && b;
        if (!isVisible)
            return new GridLength(0);

        if (parameter is string s)
            return GridLength.Parse(s);

        return GridLength.Auto;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Converts a bool to a double: true -> the value in ConverterParameter, false -> 0.
/// </summary>
public class BoolToDoubleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isVisible = value is bool b && b;
        if (!isVisible)
            return 0d;

        if (parameter is string s && double.TryParse(s, NumberStyles.Any, culture, out var d))
            return d;

        return 0d;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}