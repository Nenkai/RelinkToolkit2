using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using GBFRDataTools.FSM.Components;
using GBFRDataTools.FSM.Components.Actions.AI.Enemy;
using GBFRDataTools.FSM.Components.Actions.Quest;

using Nodify.Compatibility;

using RelinkToolkit2.Messages.Fsm;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RelinkToolkit2.ViewModels.Documents.GraphEditor.Nodes;

/// <summary>
/// Represents a fsm node.
/// </summary>
public partial class FsmNodeViewModel : FsmNodeViewModelBase //, IDropTarget
{
    [ObservableProperty]
    private string? _fsmSource;

    [ObservableProperty]
    private bool _isBranch;

    [ObservableProperty]
    private bool _hasSelfTransition;

    public string FsmFolderName { get; set; }
    public string FsmName { get; set; }
    public uint NameHash { get; set; }


    /// <summary>
    /// Parent group/layer this node belongs to.
    /// </summary>
    public FsmGroupNodeViewModel ParentGroup { get; set; }

    /// <summary>
    /// Transitions FROM this node.
    /// </summary>
    public ObservableCollection<TransitionViewModel> Transitions { get; set; } = [];
    
    /// <summary>
    /// Whether this node is the root of the layer it belongs to.
    /// </summary>
    public bool IsLayerRootNode { get; set; }

    public bool IsEndNode { get; set; }

    public FsmNodeViewModel()
    {
        if (Design.IsDesignMode)
        {
            Title = "Test Node";
            Guid = 123456789;
            HasSelfTransition = true;
            FsmSource = "my/source/file";
            LayerIndex = 1;

            AddComponent(new CallSe());
            AddComponent(new EmLockonActivate());
        }
    }

    public void AddComponent(BehaviorTreeComponent btComponent)
    {
        Components.Add(new NodeComponentViewModel(this, btComponent)
        {
            Name = btComponent.ComponentName,
        });
        
    }

    public void ClearBaseFsm()
    {
        FsmFolderName = string.Empty;
        FsmName = string.Empty;
        FsmSource = null;
    }

    public void SetBaseFsm(string fsmFolderName, string fsmName)
    {
        FsmFolderName = fsmFolderName;
        FsmName = fsmName;
        FsmSource = $"{fsmFolderName}/{fsmFolderName}_{fsmName}";
    }

    public void SetRootNodeState(bool isRootNode)
    {
        IsLayerRootNode = isRootNode;
        if (isRootNode)
            CornerRadius = new CornerRadius(3);
        else
            CornerRadius = new CornerRadius(0);

        UpdateBorderColor();
    }

    public void UpdateBorderColor()
    {
        if (IsLayerRootNode)
        {
            BorderBrush = GraphColors.NodeLayerRoot;
        }
        else if (IsEndNode)
        {
            BorderBrush = GraphColors.EndingNode;
        }
        else
        {
            if (Components.Count > 0)
                BorderBrush = GraphColors.DefaultNodeWithComponents;
            else
                BorderBrush = GraphColors.DefaultNode;
        }
    }

    [RelayCommand]
    public void OnComponentDelete(object param)
    {
        if (param is not NodeComponentViewModel nodeComponentVM)
            return;

        DeleteComponent(nodeComponentVM);
    }

    /// <summary>
    /// Deletes a component from the node.
    /// </summary>
    /// <param name="component"></param>
    public void DeleteComponent(NodeComponentViewModel component)
    {
        Components.Remove(component);

        var editor = (FsmEditorViewModel)ParentEditor;
        editor.UnregisterFsmElementGuid(component.Component.Guid);

        ((FsmNodeViewModel)component.Parent).UpdateBorderColor();
    }

    /*
    public void DragOver(IDropInfo dropInfo)
    {
        if (dropInfo.Data is ComponentTreeViewItemViewModel)
        {
            dropInfo.Effects = DragDropEffects.Move;
        }
        else
            GongSolutions.Wpf.DragDrop.DragDrop.DefaultDropHandler.DragOver(dropInfo);
    }

    public void Drop(IDropInfo dropInfo)
    {
        if (dropInfo.Data is ComponentTreeViewItemViewModel componentTvi)
        {
            AddComponentFromToolboxItem(componentTvi);
        }
        else
            GongSolutions.Wpf.DragDrop.DragDrop.DefaultDropHandler.Drop(dropInfo);
    }
    */

    [RelayCommand]
    public void OnDrop(object param)
    {
        /*
        var e = param as DragEventArgs;

        var data = e.Data.GetData("GongSolutions.Wpf.DragDrop"); // I have no idea why this is encapsulated to this.
        if (data is NodeComponentViewModel nodeComponent)
        {
            Components.Add(nodeComponent);
            nodeComponent.Parent.Components.Remove(nodeComponent);
            nodeComponent.Parent = this;
        }
        else if (data is ComponentTreeViewItemViewModel componentTvi)
        {
            AddComponentFromToolboxItem(componentTvi);
        }
        */
    }
}
