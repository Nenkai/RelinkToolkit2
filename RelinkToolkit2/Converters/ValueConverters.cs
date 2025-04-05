using Avalonia.Data.Converters;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RelinkToolkit2.Converters;

public class ValueConverters
{
    /// <summary>
    /// A value converter that returns true if the input string is null or an empty string.
    /// </summary>
    public static readonly IValueConverter NotZero =
        new FuncValueConverter<int?, bool>(x => x != 0);
}
