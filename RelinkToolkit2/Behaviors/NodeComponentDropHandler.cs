using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactions.DragAndDrop;

using DynamicData;

using Nodify;

using RelinkToolkit2.ViewModels.Documents.GraphEditor;
using RelinkToolkit2.ViewModels.Documents.GraphEditor.Nodes;

using System.Diagnostics;

namespace RelinkToolkit2.Behaviors;

public class NodeComponentDropHandler : DropHandlerBase
{
    /// <summary>
    /// Validation firing when dragging an item to the component items control itself (not the node)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="listBox"></param>
    /// <param name="e"></param>
    /// <param name="sourceContext"></param>
    /// <param name="targetContext"></param>
    /// <param name="bExecute"></param>
    /// <returns></returns>
    private bool Validate<T>(ItemsControl listBox, DragEventArgs e, object? sourceContext, object? targetContext, bool bExecute) where T : NodeComponentViewModel
    {
        if (sourceContext is not T sourceComponent || targetContext is not NodeViewModel targetNodeVm)
        {
            return false;
        }

        if (listBox.GetVisualAt(e.GetPosition(listBox)) is not Control targetControl)
            return false;

        NodeComponentViewModel targetComponent;
        if (targetControl.DataContext is NodeComponentViewModel model)
            targetComponent = model;
        else if (targetControl.Parent?.DataContext is NodeComponentViewModel)
            targetComponent = (NodeComponentViewModel)targetControl.Parent.DataContext;
        else
            return false;

        var items = targetNodeVm.Components;
        var sourceIndex = items.IndexOf(sourceComponent);
        var targetIndex = items.IndexOf(targetComponent);
        if (sourceIndex == -1) // If this is -1, then we are moving it to a different node (in the ItemsControl itself)
        {
            if (bExecute)
            {
                NodeViewModel parentNode = sourceComponent.Parent;
                parentNode.Components.Remove(sourceComponent);

                targetNodeVm.Components.Insert(targetIndex, sourceComponent);
                sourceComponent.SetParentNode(targetNodeVm);
            }
            return true;
        }
        else
        {
            if (bExecute)
                MoveItem(items, sourceIndex, targetIndex);

            return true;
        }
    }

    /// <summary>
    /// Validation firing when dragging a component to a node or other node (not the items control containing the components)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="stateNode"></param>
    /// <param name="e"></param>
    /// <param name="sourceContext"></param>
    /// <param name="targetContext"></param>
    /// <param name="bExecute"></param>
    /// <returns></returns>
    private bool Validate<T>(StateNode stateNode, DragEventArgs e, object? sourceContext, object? targetContext, bool bExecute) where T : NodeComponentViewModel
    {
        if (sourceContext is not T sourceComponent || targetContext is not NodeViewModel targetNodeVm)
            return false;

        if (sourceComponent.Parent == targetNodeVm)
            return false;

        if (targetNodeVm is FsmNodeViewModel fsmNode && !fsmNode.CanHaveComponents)
            return false;

        if (stateNode.GetVisualAt(e.GetPosition(stateNode)) is not Control targetControl)
            return false;

        NodeComponentViewModel? targetComponent = null;
        if (targetControl.DataContext is NodeComponentViewModel model)
            targetComponent = model;
        else if (targetControl.Parent?.DataContext is NodeComponentViewModel)
            targetComponent = (NodeComponentViewModel)targetControl.Parent.DataContext;

        var items = targetNodeVm.Components;
        if (bExecute)
        {
            NodeViewModel parentNode = sourceComponent.Parent;
            parentNode.Components.Remove(sourceComponent);

            if (targetComponent is not null)
            {
                var targetIndex = items.IndexOf(targetComponent);
                targetNodeVm.Components.Insert(targetIndex, sourceComponent);
            }
            else
            {
                targetNodeVm.Components.Add(sourceComponent);
            }

            sourceComponent.SetParentNode(targetNodeVm);
        }

        return true;
    }

    public override bool Validate(object? sender, DragEventArgs e, object? sourceContext, object? targetContext, object? state)
    {
        if (sourceContext is not NodeComponentViewModel)
            return false;

        if (sender is ItemsControl listBox)
        {
            return Validate<NodeComponentViewModel>(listBox, e, sourceContext, targetContext, false);
        }
        else if (sender is StateNode stateNode)
        {
            return Validate<NodeComponentViewModel>(stateNode, e, sourceContext, targetContext, false);
        }

        return false;
    }

    public override bool Execute(object? sender, DragEventArgs e, object? sourceContext, object? targetContext, object? state)
    {
        if (sender is ItemsControl listBox)
        {
            return Validate<NodeComponentViewModel>(listBox, e, sourceContext, targetContext, true);
        }
        else if (sender is StateNode stateNode)
        {
            return Validate<NodeComponentViewModel>(stateNode, e, sourceContext, targetContext, true);
        }

        return false;
    }
}
