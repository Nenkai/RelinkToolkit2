using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using GBFRDataTools.Entities;
using GBFRDataTools.FSM;
using GBFRDataTools.FSM.BehaviorTree;
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
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RelinkToolkit2.ViewModels.Documents.GraphEditor.Nodes;

/// <summary>
/// Represents a fsm node.
/// </summary>
public partial class FsmNodeViewModel : FsmNodeViewModelBase //, IDropTarget
{
    [ObservableProperty]
    public partial string? FsmSource { get; set; }

    [ObservableProperty]
    public partial bool IsBranch { get; set; }

    [ObservableProperty]
    public partial bool HasSelfTransition { get; set; }

    public string? FsmFolderName { get; set; }
    public string? FsmName { get; set; }
    public uint NameHash { get; set; }

    /// <summary>
    /// Parent group/layer this node belongs to.
    /// </summary>
    [JsonIgnore]
    public FsmGroupNodeViewModel ParentGroup { get; set; }

    /// <summary>
    /// Transitions FROM this node.
    /// </summary>
    [JsonIgnore]
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

    public FsmNodeViewModel CloneAsNewNode()
    {
        var editor = (FsmEditorViewModel)ParentEditor;

        var node = new FsmNodeViewModel()
        {
            Anchor = Anchor,
            Location = Location + new Point(50, 50),
            Guid = editor.GetNewGuid(),
            Title = Title + " - Copy",
            Size = Size,
            NameHash = NameHash,

            ParentEditor = ParentEditor,
            FsmSource = FsmSource,
            LayerIndex = LayerIndex,
            FsmFolderName = FsmFolderName,
            FsmName = FsmName,
            ParentGroup = ParentGroup,
            IsEndNode = IsEndNode,
            IsBranch = IsBranch,
        };

        foreach (NodeComponentViewModel srcComponent in Components)
        {
            var clonedComponent = srcComponent.CloneAsNewComponent(node);
            node.Components.Add(clonedComponent);
        }

        node.UpdateVisuals();

        return node;
    }
    
    /// <summary>
    /// Returns whether this node has a base FSM link.
    /// </summary>
    /// <returns></returns>
    public bool HasBaseFSMLink()
    {
        return !string.IsNullOrWhiteSpace(FsmName) && !string.IsNullOrWhiteSpace(FsmFolderName);
    }

    public bool CanSetAsEndNode()
    {
        return !IsEndNode && !IsLayerRootNode && Components.Count == 0 && Transitions.Count == 0 && !HasBaseFSMLink();
    }

    /// <summary>
    /// Returns whether this node is allowed to have components
    /// </summary>
    /// <returns></returns>
    public bool CanHaveComponents()
    {
        return !IsEndNode && // End nodes don't physically exist therefore they cannot have components
            !HasBaseFSMLink() && // Nodes that call fsm files don't seem to have components
            !IsLayerRootNode; // Root nodes cannot have components it seems
    }

    public void AddComponent(BehaviorTreeComponent btComponent)
    {
        Components.Add(new NodeComponentViewModel(this, btComponent)
        {
            Name = btComponent.ComponentName,
        });

        // Visuals will be updated with collection change
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

        UpdateVisuals();
    }

    public override void UpdateVisuals()
    {
        if (IsLayerRootNode)
            CornerRadius = new CornerRadius(3);
        else
            CornerRadius = new CornerRadius(0);

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
