using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using Avalonia.PropertyGrid.Controls;
using Avalonia.Styling;

using RelinkToolkit2.Controls.PropertyGrid.ViewModel;
using RelinkToolkit2.ViewModels.Menu;

using System.Collections.ObjectModel;

namespace RelinkToolkit2.Controls.PropertyGrid.Views;

/// <summary>
/// Interaction logic for PropertyGridView.xaml
/// </summary>
public partial class DocumentablePropertyGridView : UserControl
{
    public DocumentablePropertyGridView()
    {
        InitializeComponent();
    }

    private void PropertyGrid_CustomNameBlock(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var args = (CustomNameBlockEventArgs)e;
        var vm = (DocumentablePropertyGridViewModel)DataContext!;

        var textBlock = new TextBlock
        {
            Padding = new Thickness(4),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            Background = Brushes.Transparent,
            ContextMenu = new ExtendableContextMenu
            {
                ItemsSource = new ObservableCollection<MenuItemViewModel>()
                {
                    new()
                    {
                        Header = "View/Edit documentation...",
                        Enabled = true,
                        IconKind = "Material.CommentEdit",
                        Command = vm.EditPropertyDocumentationCommand,
                        Parameter = args.Context.Property
                    }
                }
            }
        };

        textBlock.PointerEntered += (ss, ee) => vm.OnPointerEnteredPropertyName(args.Context);
        textBlock.PointerExited += (ss, ee) => vm.OnPointerExitedPropertyName(args.Context);
        textBlock.PointerPressed += async (ss, ee) =>
        {
            if (ee.ClickCount != 2)
            {
                ee.Handled = true;
                return;
            }


            await vm.OnEditPropertyDocumentation(args.Context.Property);
        };

        textBlock.Text = args.Context.DisplayName;
        args.CustomNameBlock = textBlock;
    }
}
