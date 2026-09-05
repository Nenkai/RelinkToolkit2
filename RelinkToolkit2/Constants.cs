using Avalonia.Media;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RelinkToolkit2;

public class GraphColors
{
    public static IBrush DefaultNodeBackground => Brushes.Black;
    public static IBrush DefaultNodeBorder => Brushes.Black;
    public static IBrush DefaultNodeWithComponents => Brushes.DimGray;

    public static IBrush NodeLayerRoot => Brushes.Green;

    public static IBrush EndNodeBackground => Brushes.DarkRed;
    public static IBrush EndNodeBorder => Brushes.DarkRed;

    public static IBrush BranchNode => new SolidColorBrush(Color.FromRgb(0xD3, 0x7A, 0x00), 1.0d);

    public static IBrush NormalTransition => Brushes.DodgerBlue;
    public static IBrush OverrideTransition => Brushes.OrangeRed;

    public static IBrush ComponentBorderHighlight => Brushes.Orange;
    public static IBrush ComponentBorderNormal => Brushes.DarkGray;

    public static IBrush ConditionGroupAnd => Brushes.YellowGreen;
    public static IBrush ConditionGroupOr => Brushes.DeepSkyBlue;
    public static IBrush ConditionGroupNone => Brushes.Black;

    public static IBrush BTDecorationNode => Brushes.Orange;
    public static IBrush BTRootNode => Brushes.Green;
    public static IBrush BTActionNode => Brushes.Blue;
}
