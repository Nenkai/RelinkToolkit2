using Avalonia.Controls;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using Dock.Model.Mvvm.Controls;

using RelinkToolkit2.Messages;
using RelinkToolkit2.Messages.Fsm;
using RelinkToolkit2.ViewModels.Documents;
using RelinkToolkit2.ViewModels.Documents.GraphEditor;
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
    private readonly Dictionary<Guid, TreeViewItemViewModel> _idToItem = [];
    public ObservableCollection<TreeViewItemViewModel> DisplayedItems { get; set; } = [];

    public SolutionExplorerViewModel()
    {
        Id = "SolutionExplorer";
        Title = "Solution Explorer";

        if (Design.IsDesignMode)
        {
            var editor = new FsmEditorViewModel();

            AddItem(new FSMTreeViewItemViewModel()
            {
                TreeViewName = "FSM",
                FsmEditor = new(),
                DisplayedItems =
                [
                    new FSMLayerTreeViewItemViewModel()
                    {
                        LayerGroup = new() { ParentEditor = editor, },
                        TreeViewName = "Layer 0",
                        Caption = "Test!",
                    },
                    new FSMLayerTreeViewItemViewModel()
                    {
                        LayerGroup = new() { ParentEditor = editor, },
                        TreeViewName = "Layer 1",
                    },
                    new FSMLayerTreeViewItemViewModel()
                    {
                        LayerGroup = new() { ParentEditor = editor, },
                        TreeViewName = "Layer 2",
                    },
                ]
            });
        }
    }

    
    /// <summary>
    /// Adds an item with the specified id.
    /// </summary>
    /// <param name="id">Id of the tree view item.</param>
    /// <param name="item">Item to add.</param>
    /// <param name="parentId">Parent item to add to.</param>
    public void AddItem(TreeViewItemViewModel item, Guid? parentId = null)
    {
        if (parentId is not null && _idToItem.TryGetValue((Guid)parentId, out TreeViewItemViewModel? parentItem))
        {
            if (parentItem!.DisplayedItems.Contains(item))
                return;

            parentItem.DisplayedItems.Add(item);
            item.Parent = parentItem;

            _idToItem.Add(item.Guid, item);

            RegisterSubTreeItem(item);
        }
        else
        {
            // Doesn't exist, add the root
            DisplayedItems.Add(item);
            _idToItem.Add(item.Guid, item);

            RegisterSubTreeItem(item);
        }
    }

    public TreeViewItemViewModel? GetItem(Guid guid)
    {
        _idToItem.TryGetValue(guid, out TreeViewItemViewModel? item);
        return item;
    }

    /// <summary>
    /// Removes a node and its children from the tree.
    /// </summary>
    /// <param name="id"></param>
    public void RemoveItem(Guid guid)
    {
        if (_idToItem.TryGetValue(guid, out TreeViewItemViewModel? item))
        {
            _idToItem.Remove(guid);
            UnregisterSubTreeItem(item);

            item.Parent?.DisplayedItems.Remove(item);
            DisplayedItems.Remove(item);
        }
    }


    private void RegisterSubTreeItem(TreeViewItemViewModel parent)
    {
        foreach (TreeViewItemViewModel ivm in parent.DisplayedItems)
        {
            _idToItem.TryAdd(ivm.Guid, ivm);
            RegisterSubTreeItem(ivm);
        }
    }

    private void UnregisterSubTreeItem(TreeViewItemViewModel parent)
    {
        foreach (TreeViewItemViewModel ivm in parent.DisplayedItems)
        {
            _idToItem.Remove(ivm.Guid);
            UnregisterSubTreeItem(ivm);
        }
    }

    public void Clear()
    {
        _idToItem.Clear();
        DisplayedItems.Clear();
    }
}
