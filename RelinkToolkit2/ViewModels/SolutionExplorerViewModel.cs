using Avalonia.Controls;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using Dock.Model.Mvvm.Controls;

using RelinkToolkit2.Messages;
using RelinkToolkit2.Messages.Fsm;
using RelinkToolkit2.ViewModels.Documents;
using RelinkToolkit2.ViewModels.TreeView;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace RelinkToolkit2.ViewModels;

public partial class SolutionExplorerViewModel : Tool
{
    private readonly Dictionary<string, TreeViewItemViewModel> _idToItem = [];
    public ObservableCollection<TreeViewItemViewModel> DisplayedItems { get; set; } = [];

    public SolutionExplorerViewModel()
    {
        Id = "SolutionExplorer";
        Title = "Solution Explorer";

        if (Design.IsDesignMode)
        {
            var editor = new FsmEditorViewModel();

            AddItem("one", new FSMTreeViewItemViewModel()
            {
                Id = "one",
                TreeViewName = "FSM",
                FsmEditor = new(),
                DisplayedItems =
                [
                    new FSMLayerTreeViewItemViewModel()
                    {
                        LayerGroup = new() { ParentEditor = editor, },
                        TreeViewName = "Layer",
                    },
                    new FSMLayerTreeViewItemViewModel()
                    {
                        LayerGroup = new() { ParentEditor = editor, },
                        TreeViewName = "Layer",
                    },
                    new FSMLayerTreeViewItemViewModel()
                    {
                        LayerGroup = new() { ParentEditor = editor, },
                        TreeViewName = "Layer",
                    },
                ]
            });
        }
    }

    

    public void AddItem(string id, TreeViewItemViewModel item)
    {
        if (_idToItem.TryAdd(id, item))
        {
            DisplayedItems.Add(item);

            foreach (TreeViewItemViewModel tvi in item.DisplayedItems)
            {
                RegisterSubTreeItem(tvi);
            }
        }
    }

    public void RegisterSubTreeItem(TreeViewItemViewModel parent)
    {
        foreach (TreeViewItemViewModel ivm in parent.DisplayedItems)
        {
            _idToItem.Add(ivm.Id, ivm);
            RegisterSubTreeItem(ivm);
        }
    }

    public void Clear()
    {
        _idToItem.Clear();
        DisplayedItems.Clear();
    }
}
