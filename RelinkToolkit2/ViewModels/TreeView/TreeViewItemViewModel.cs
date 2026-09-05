using CommunityToolkit.Mvvm.ComponentModel;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Avalonia;
using System.Windows.Input;

namespace RelinkToolkit2.ViewModels.TreeView;

public partial class TreeViewItemViewModel : ObservableObject
{
    [ObservableProperty]
    public partial Guid Guid { get; set; } = Guid.CreateVersion7();

    [ObservableProperty]
    public partial string Caption { get; set; }

    [ObservableProperty]
    public partial string TreeViewName { get; set; } = "No Name";

    [ObservableProperty]
    public partial string? IconKind { get; set; }

    [ObservableProperty]
    public partial bool Visible { get; set; } = true;

    [ObservableProperty]
    public partial bool IsExpanded { get; set; } = false;

    [ObservableProperty]
    public partial bool CanDrop { get; set; }

    [ObservableProperty]
    public partial ICommand? DoubleClickedCommand { get; set; }

    public TreeViewItemViewModel? Parent { get; set; }


    /// <summary>
    /// Sub-tree items.<br/>
    /// <br/>
    /// You should not add directly to this (unless this item is the root).
    /// </summary>
    public ObservableCollection<TreeViewItemViewModel> DisplayedItems { get; set; } = [];
}
