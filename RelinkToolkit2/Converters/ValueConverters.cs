using Avalonia.Data.Converters;
using Avalonia.Utilities;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RelinkToolkit2.Converters;

public static class ValueConverters
{
    public static readonly IValueConverter NotZero = new FuncValueConverter<int?, bool>(x => x != 0);
    public static readonly IValueConverter NotMinusOne = new FuncValueConverter<object?, bool>(val =>
    {
        if (val is null)
            return false;

        int value = (int)val;
        return value != (-1);
    });
}
