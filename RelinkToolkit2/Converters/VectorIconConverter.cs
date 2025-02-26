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

public class VectorIconConverter : IValueConverter
{
    public static readonly VectorIconConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter,
                                                            CultureInfo culture)
    {
        if (value is null)
            return null;

        if (value is not string)
            return new BindingNotification(new InvalidCastException("Expected a string for vector icon drawing conversion"), BindingErrorType.Error);

        if (App.Current is not null)
        {
            foreach (var style in App.Current.Styles)
            {
                if (style.TryGetResource(value, App.Current.ActualThemeVariant, out var val))
                {
                    return val;
                }
            }
        }

        // converter used for the wrong type
        return new BindingNotification(new KeyNotFoundException(), BindingErrorType.Error);
    }

    public object ConvertBack(object? value, Type targetType,
                                object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
