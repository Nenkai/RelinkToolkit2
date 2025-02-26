using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Media;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using GBFRDataTools.FSM.Entities;

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
    public int _guid;

    [ObservableProperty]
    public int _layerIndex;

    [ObservableProperty]
    private Size _size;

    [ObservableProperty]
    private string _fsmSource;

    public ObservableCollection<NodeComponentViewModel> Components { get; set; } = [];

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

        Components.Remove(nodeComponentVM);
    }

    private void AddComponentFromToolboxItem(ComponentTreeViewItemViewModel componentTvi)
    {
        BehaviorTreeComponent component = (BehaviorTreeComponent)Activator.CreateInstance(componentTvi.ComponentType)!;
        component.ComponentName = componentTvi.TreeViewName;
        Components.Add(new NodeComponentViewModel(this)
        {
            Component = component,
            Name = component.ComponentName,
        });
    }
}
