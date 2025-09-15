using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using Avalonia.PropertyGrid.Controls;

using RelinkToolkit2.ViewModels;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RelinkToolkit2.Views;

/// <summary>
/// Interaction logic for PropertyGridView.xaml
/// </summary>
public partial class PropertyGridView : UserControl
{
    public PropertyGridView()
    {
        InitializeComponent();
    }

    private void PropertyGrid_CustomNameBlock(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var args = (CustomNameBlockEventArgs)e;

        var textBlock = new TextBlock()
        {
            Padding = new Thickness(4),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Background = Brushes.Transparent,
        };

        textBlock.PointerEntered += (ss, ee) =>
        {
            var vm = (PropertyGridViewModel)this.DataContext!;
            vm.OnPointerEnteredPropertyName(args.Context);
        };
        textBlock.PointerExited += (ss, ee) =>
        {
            var vm = (PropertyGridViewModel)this.DataContext!;
            vm.OnPointerExitedPropertyName(args.Context);
        };
        textBlock.Text = args.Context.DisplayName;
        args.CustomNameBlock = textBlock;
    }
}
