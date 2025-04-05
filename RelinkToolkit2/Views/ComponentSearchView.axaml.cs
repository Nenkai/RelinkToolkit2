using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

using PropertyModels.Extensions;

using RelinkToolkit2.ViewModels;
using RelinkToolkit2.ViewModels.Search;

using System.Linq;

namespace RelinkToolkit2.Views;

public partial class ComponentSearchView : UserControl
{
    public ComponentSearchView()
    {
        InitializeComponent();
    }

    private void PreviousImage_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        GroupSlides.Previous();
    }

    private void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        GroupSlides.Next();
    }

    private void Grid_PointerPressed_1(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0)
            return;

        if (e.Source is Control control)
        {
            var dataContext = e.AddedItems[0];

            if (dataContext is ComponentSearchItemViewModel itemVM)
            {
                ComponentSearchViewModel searchVm = (this.DataContext as ComponentSearchViewModel)!;

                if (itemVM.Data is ComponentSearchPageViewModel pageVM)
                {
                    if (searchVm.Pages.Count >= searchVm.PageIndex + 2)
                        searchVm.Pages[searchVm.PageIndex + 1] = pageVM;
                    else
                        searchVm.AddPage(pageVM);
                    searchVm.PageIndex++;
                }
                else
                {
                    itemVM.OnSelected?.Invoke(searchVm, itemVM);

                    // Don't blame me for doing.. Whatever that is.
                    // It's not possible to bind Flyout's IsOpen when it's created manually, so no way to close it on the view model side.
                    // I really tried to bind it programatically. It's just not possible. Flyout doesn't have a data context.
                    ILogical? popupLogical = control.FindLogicalAncestorOfType<Popup>();
                    if (popupLogical is not Popup popup)
                        return;

                    popup.Close();
                }
            }
        }
    }
}