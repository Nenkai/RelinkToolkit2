using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using Avalonia.Controls;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using GBFRDataTools.FSM.Components;
using GBFRDataTools.FSM.Components.Actions.AI.Enemy;
using GBFRDataTools.FSM.Components.Actions.Quest;

namespace RelinkToolkit2.ViewModels.Documents.GraphEditor.Nodes;

/// <summary>
/// Represents a fsm node.
/// </summary>
public partial class BTNodeViewModel : NodeViewModel //, IDropTarget
{    
    public bool IsEndNode { get; set; }

    // Preferably this should be removed in favor of viewmodels/views for EACH type of node.
    [ObservableProperty]
    private string _description;

    public BTNodeViewModel()
    {
        Title = NodeViewModel.DefaultNodeName;

        if (Design.IsDesignMode)
        {
            Title = "Test Node";
            Guid = 123456789;

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
        UpdateVisuals();
    }

    public override void UpdateVisuals()
    {
        
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
