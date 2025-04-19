using CommunityToolkit.Mvvm.ComponentModel;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Avalonia;
using System.Windows.Input;

namespace RelinkToolkit2.ViewModels;

public partial class TreeViewItemViewModel : ObservableObject
{
    [ObservableProperty]
    private Guid _guid = Guid.CreateVersion7();

    [ObservableProperty]
    private string _caption;

    [ObservableProperty]
    private string _treeViewName = "No Name";

    [ObservableProperty]
    private string? _iconKind;

    [ObservableProperty]
    private bool _visible = true;

    [ObservableProperty]
    private bool _isExpanded = false;

    [ObservableProperty]
    private bool _canDrop;

    [ObservableProperty]
    private ICommand? _doubleClickedCommand;

    public TreeViewItemViewModel? Parent { get; set; }


    /// <summary>
    /// Sub-tree items.<br/>
    /// <br/>
    /// You should not add directly to this (unless this item is the root).
    /// </summary>
    public ObservableCollection<TreeViewItemViewModel> DisplayedItems { get; set; } = [];
}
