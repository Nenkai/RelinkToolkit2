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

    [ObservableProperty]
    private ICommand? _doubleClickedCommand;

    public ObservableCollection<TreeViewItemViewModel> DisplayedItems { get; set; } = [];
    private Dictionary<string, TreeViewItemViewModel> _keyDictionary { get; set; } = [];

    public void AddChild(TreeViewItemViewModel itemViewModel)
    {
        DisplayedItems.Add(itemViewModel);
        _keyDictionary.Add(itemViewModel.Id, itemViewModel);
    }

    public void GetChild()
    {

    }

    public void RemoveChild(TreeViewItemViewModel itemViewModel)
    {
        DisplayedItems.Remove(itemViewModel);
        _keyDictionary.Remove(itemViewModel.Id);
    }
}
