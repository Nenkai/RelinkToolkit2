using CommunityToolkit.Mvvm.ComponentModel;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Avalonia;

namespace RelinkToolkit2.ViewModels;

public partial class TreeViewItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _id;

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

    public ObservableCollection<TreeViewItemViewModel> DisplayedItems { get; set; } = [];
}
