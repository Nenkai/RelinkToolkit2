using Avalonia.Data.Converters;
using Avalonia.Data;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Styling;
using Avalonia;

namespace RelinkToolkit2.Converters;

public class HexTextConverter : IValueConverter
{
    public static readonly VectorIconConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter,
                                                            CultureInfo culture)
    {
        if (value is not string str)
            return null;

        if (!int.TryParse(str, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int hexValue))
            return null;

        return (decimal)hexValue;
    }

    public object ConvertBack(object? value, Type targetType,
                                object? parameter, CultureInfo culture)
    {
        if (value is null)
            return string.Empty;

        return decimal.ToInt32((decimal)value).ToString("X4");
    }
}
