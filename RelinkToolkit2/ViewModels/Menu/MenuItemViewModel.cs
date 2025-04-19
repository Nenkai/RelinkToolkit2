using CommunityToolkit.Mvvm.ComponentModel;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Windows.Input;

using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Input;

namespace RelinkToolkit2.ViewModels.Menu;

public partial class MenuItemViewModel : ObservableObject, IMenuItemViewModel
{
    public static readonly MenuItemViewModel Separator;

    static MenuItemViewModel()
    {
        Separator = new MenuItemViewModel()
        {
            Header = "-",
        };
    }

    [ObservableProperty]
    private string? _header;

    [ObservableProperty]
    private FontWeight _fontWeight = FontWeight.Regular;

    [ObservableProperty]
    private bool _checked;

    [ObservableProperty]
    private bool _enabled;

    [ObservableProperty]
    private MenuItemToggleType _toggleType;

    [ObservableProperty]
    private ICommand? _command;

    [ObservableProperty]
    private object? _parameter;

    [ObservableProperty]
    private string? _iconKind;

    [ObservableProperty]
    private IBrush? _iconBrush;

    [ObservableProperty]
    private bool _isHitTestVisible = true;

    [ObservableProperty]
    private KeyGesture? _hotKey;

    [ObservableProperty]
    private bool _isSubMenuOpen;

    [ObservableProperty]
    private bool _staysOpenOnClick;


    public ObservableCollection<IMenuItemViewModel> MenuItems { get; set; } = [];
}
