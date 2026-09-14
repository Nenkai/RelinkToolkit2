using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using Avalonia.PropertyGrid.Controls;
using Avalonia.PropertyGrid.ViewModels;
using Avalonia.Styling;

using ColorTextBlock.Avalonia;

using GBFRDataTools.FSM.Components;

using Microsoft.Extensions.DependencyInjection;

using RelinkToolkit2.Controls.PropertyGrid.ViewModel;
using RelinkToolkit2.Services;
using RelinkToolkit2.ViewModels.Documents.GraphEditor.TransitionComponents;
using RelinkToolkit2.ViewModels.Menu;

using System.Collections.ObjectModel;
using System.ComponentModel;

namespace RelinkToolkit2.Controls.PropertyGrid.Views;

/// <summary>
/// Interaction logic for PropertyGridView.xaml
/// </summary>
public partial class DocumentablePropertyGridView : UserControl
{
    public static readonly StyledProperty<bool> IsQuickFilterVisibleProperty =
        AvaloniaProperty.Register<DocumentablePropertyGridView, bool>(nameof(IsQuickFilterVisible), defaultValue: true);

    public bool IsQuickFilterVisible
    {
        get => GetValue(IsQuickFilterVisibleProperty);
        set => SetValue(IsQuickFilterVisibleProperty, value);
    }

    public static readonly StyledProperty<bool> IsTitleVisibleProperty =
        AvaloniaProperty.Register<DocumentablePropertyGridView, bool>(nameof(IsTitleVisible), defaultValue: true);

    public bool IsTitleVisible
    {
        get => GetValue(IsTitleVisibleProperty);
        set => SetValue(IsTitleVisibleProperty, value);
    }

    public static readonly StyledProperty<bool> ShowDocTooltipOnHoverProperty =
        AvaloniaProperty.Register<DocumentablePropertyGridView, bool>(nameof(ShowDocTooltipOnHover), defaultValue: true);

    public bool ShowDocTooltipOnHover
    {
        get => GetValue(ShowDocTooltipOnHoverProperty);
        set => SetValue(ShowDocTooltipOnHoverProperty, value);
    }

    public static readonly StyledProperty<bool> IsHeaderVisibleProperty =
        AvaloniaProperty.Register<DocumentablePropertyGridView, bool>(nameof(IsHeaderVisible), defaultValue: true);

    public bool IsHeaderVisible
    {
        get => GetValue(IsHeaderVisibleProperty);
        set => SetValue(IsHeaderVisibleProperty, value);
    }

    public static readonly StyledProperty<bool> DocumentationPanelVisibleProperty =
        AvaloniaProperty.Register<DocumentablePropertyGridView, bool>(nameof(DocumentationPanelVisible), defaultValue: true);

    public bool DocumentationPanelVisible
    {
        get => GetValue(DocumentationPanelVisibleProperty);
        set => SetValue(DocumentationPanelVisibleProperty, value);
    }

    public static readonly StyledProperty<bool> IsCategoryVisibleProperty =
        AvaloniaProperty.Register<DocumentablePropertyGridView, bool>(nameof(IsCategoryVisible), defaultValue: true);
    public bool IsCategoryVisible
    {
        get => GetValue(IsCategoryVisibleProperty);
        set => SetValue(IsCategoryVisibleProperty, value);
    }

    public static readonly StyledProperty<PropertyGridLayoutStyle> LayoutStyleProperty =
        AvaloniaProperty.Register<DocumentablePropertyGridView, PropertyGridLayoutStyle>(nameof(LayoutStyle), defaultValue: PropertyGridLayoutStyle.Tree);

    public PropertyGridLayoutStyle LayoutStyle
    {
        get => GetValue(LayoutStyleProperty);
        set => SetValue(LayoutStyleProperty, value);
    }

    public static readonly StyledProperty<PropertyGridCategorizingStyle> PropertyCategorizingStyleProperty =
        AvaloniaProperty.Register<DocumentablePropertyGridView, PropertyGridCategorizingStyle>(nameof(PropertyCategorizingStyle), defaultValue: PropertyGridCategorizingStyle.Normal);

    public PropertyGridCategorizingStyle PropertyCategorizingStyle
    {
        get => GetValue(PropertyCategorizingStyleProperty);
        set => SetValue(PropertyCategorizingStyleProperty, value);
    }

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
            DataContext = args.Context.Property,
            ContextMenu = new ExtendableContextMenu
            {
                ItemsSource = new ObservableCollection<MenuItemViewModel>()
                {
                    new()
                    {
                        Header = "View/edit documentation...",
                        Enabled = true,
                        IconKind = "Material.CommentEdit",
                        Command = vm.EditPropertyDocumentationCommand,
                        Parameter = args.Context.Property
                    }
                }
            }
        };

        
        textBlock.SetValue(ToolTip.TipProperty, "placeholder");
        textBlock.AddHandler(ToolTip.ToolTipOpeningEvent, ConditionComponentTitle_OnToolTipOpening);

        textBlock.PointerEntered += (ss, ee) =>
        {
            vm.OnPointerEnteredPropertyName(args.Context);
        };

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

    private void ConditionComponentTitle_OnToolTipOpening(object? sender, Avalonia.Interactivity.CancelRoutedEventArgs e)
    {
        if (sender is not Control control)
            return;

        if (!ShowDocTooltipOnHover)
        {
            e.Cancel = true;
            return;
        }

        var propertyDescriptor = (PropertyDescriptor)control.DataContext!;

        ComponentDocumentationService docService = App.Current.Services.GetRequiredService<ComponentDocumentationService>();
        string tip = $"#### {propertyDescriptor.Name}\n\n";
        string? docString = docService.GetPropertyString(propertyDescriptor);
        if (!string.IsNullOrWhiteSpace(docString))
            tip += docString;
        else
            tip += $"**No documentation was found for property **`{propertyDescriptor.Name}`.\n\n(You can add one by double-clicking this property)";

        var stackPanel = new StackPanel();
        var markdown = new Markdown.Avalonia.MarkdownScrollViewer()
        {
            Markdown = tip
        };

        var style = new Style(x => x.OfType<CTextBlock>());
        style.Setters.Add(new Setter(CTextBlock.FontFamilyProperty, new FontFamily("Segoe UI")));
        markdown.Styles.Add(style);

        stackPanel.Children.Add(markdown);

        ToolTip.SetTip(control, stackPanel);
    }
}
