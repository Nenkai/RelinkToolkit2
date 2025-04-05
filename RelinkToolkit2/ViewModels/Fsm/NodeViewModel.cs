﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GBFRDataTools.FSM.Components.Actions.Quest;
using GBFRDataTools.FSM.Components.Actions.Enemy;
using GBFRDataTools.FSM.Components;

namespace RelinkToolkit2.ViewModels.Fsm;

/// <summary>
/// Represents a node on the graph.
/// </summary>
public partial class NodeViewModel : NodeViewModelBase //, IDropTarget
{
    [ObservableProperty]
    private Point _anchor;

    [ObservableProperty]
    private IBrush _borderBrush = Brushes.DimGray;

    [ObservableProperty]
    private CornerRadius _cornerRadius = new(5);

    [ObservableProperty]
    private uint _guid;

    [ObservableProperty]
    private string? _fsmSource;

    [ObservableProperty]
    private bool _hasSelfTransition;

    [ObservableProperty]
    private bool _isRenaming;

    public string FsmFolderName { get; set; }
    public string FsmName { get; set; }
    public uint NameHash { get; set; }

    /// <summary>
    /// Transitions FROM this node.
    /// </summary>
    public ObservableCollection<TransitionViewModel> Transitions { get; set; } = [];

    /// <summary>
    /// Execution components.
    /// </summary>
    public ObservableCollection<NodeComponentViewModel> Components { get; set; } = [];

    /// <summary>
    /// Whether this node is the root of the layer it belongs to.
    /// </summary>
    public bool IsLayerRootNode { get; set; }

    public bool IsEndNode { get; set; }

    public NodeViewModel()
    {
        if (Design.IsDesignMode)
        {
            Title = "Test Node";
            Guid = 123456789;
            HasSelfTransition = true;
            FsmSource = "my/source/file";
            Components =
            [
                new NodeComponentViewModel(this)
                {
                    Component = new CallSe(),
                },
                new NodeComponentViewModel(this)
                {
                    Component = new EmLockonActivate(),
                }
            ];
        }
    }

    public void RemoveAllTransitionsWithGuid(uint guid)
    {
        for (int i = Transitions.Count - 1; i >= 0; i--)
        {
            if (Transitions[i].Source.Guid == guid || Transitions[i].Target.Guid == guid)
                Transitions.Remove(Transitions[i]);
        }
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
        ParentEditor.UnregisterFsmElementGuid(component.Component.Guid);
    }

    private void AddComponentFromToolboxItem(ComponentTreeViewItemViewModel componentTvi)
    {
        BehaviorTreeComponent component = (BehaviorTreeComponent)Activator.CreateInstance(componentTvi.ComponentType)!;
        Components.Add(new NodeComponentViewModel(this)
        {
            Component = component,
            Name = component.ComponentName,
        });
    }
}
