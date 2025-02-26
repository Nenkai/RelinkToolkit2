using Avalonia.Media;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RelinkToolkit2;

public class GraphColors
{
    public static IBrush EndingNode => Brushes.DarkRed;

    public static IBrush NormalTransition => Brushes.DodgerBlue;
    public static IBrush UnkTransition => Brushes.Green;

    public static IBrush ComponentBorderHighlight => Brushes.Orange;
    public static IBrush ComponentBorderNormal => Brushes.DarkGray;
}
