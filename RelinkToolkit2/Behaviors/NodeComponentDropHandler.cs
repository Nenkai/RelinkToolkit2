using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactions.DragAndDrop;

using RelinkToolkit2.ViewModels.Documents.GraphEditor;
using RelinkToolkit2.ViewModels.Documents.GraphEditor.Nodes;

namespace RelinkToolkit2.Behaviors;

public class NodeComponentDropHandler : DropHandlerBase
{
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
        if (sourceIndex == -1) // If this is -1, then we are moving it to a different node
        {
            if (bExecute)
            {
                NodeViewModel parentNode = sourceComponent.Parent;
                parentNode.Components.Remove(sourceComponent);
                targetNodeVm.Components.Insert(targetIndex, sourceComponent);
                sourceComponent.Parent = targetNodeVm; // Make sure to adjust the parent
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
        
    public override bool Validate(object? sender, DragEventArgs e, object? sourceContext, object? targetContext, object? state)
    {
        if (sender is ItemsControl listBox)
        {
            return Validate<NodeComponentViewModel>(listBox, e, sourceContext, targetContext, false);
        }

        return false;
    }

    public override bool Execute(object? sender, DragEventArgs e, object? sourceContext, object? targetContext, object? state)
    {
        if (sender is ItemsControl listBox)
        {
            return Validate<NodeComponentViewModel>(listBox, e, sourceContext, targetContext, true);
        }
        return false;
    }
}
