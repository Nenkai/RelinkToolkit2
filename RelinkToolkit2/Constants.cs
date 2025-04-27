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
    public static IBrush DefaultNode => Brushes.Black;
    public static IBrush DefaultNodeWithComponents => Brushes.DimGray;
    public static IBrush NodeLayerRoot => Brushes.Green;
    public static IBrush NormalTransition => Brushes.DodgerBlue;
    public static IBrush OverrideTransition => Brushes.OrangeRed;

    public static IBrush ComponentBorderHighlight => Brushes.Orange;
    public static IBrush ComponentBorderNormal => Brushes.DarkGray;

    public static IBrush BTDecorationNode => Brushes.Orange;
    public static IBrush BTRootNode => Brushes.Green;
    public static IBrush BTActionNode => Brushes.Blue;
}
