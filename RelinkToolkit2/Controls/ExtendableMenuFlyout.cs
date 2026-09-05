using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;

using RelinkToolkit2.ViewModels.Menu;

namespace RelinkToolkit2.Controls;

/// <summary>
/// Simple, extendable menu flyout through <see cref="MenuItemViewModel"/>
/// </summary>
public class ExtendableMenuFlyout : MenuFlyout
{
    public ExtendableMenuFlyout() 
        : base()
    {
        // MenuItemViewModelTopLevelTheme is included in App.axaml.cs, defined in Controls/Menu/MenuItemContainerTheme
        ItemContainerTheme = (ControlTheme)Application.Current!.FindResource("MenuItemViewModelTheme");
    }
}
