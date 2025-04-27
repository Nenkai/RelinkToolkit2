using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactions.DragAndDrop;

using RelinkToolkit2.ViewModels.Documents.GraphEditor.TransitionComponents;
using RelinkToolkit2.ViewModels.Documents.GraphEditor;

namespace RelinkToolkit2.Behaviors;

public class NewTransitionComponentDropHandler : DropHandlerBase
{
    private bool Validate<T>(ItemsControl listBox, DragEventArgs e, object? sourceContext, object? targetContext, bool bExecute) where T : TransitionConditionViewModel
    {
        if (sourceContext is not T sourceComponent || targetContext is not TransitionViewModel targetTransitionViewModel)
        {
            return false;
        }

        if (listBox.GetVisualAt(e.GetPosition(listBox)) is not Control targetControl)
            return false;

        if (targetControl.DataContext is not TransitionConditionViewModel targetComponent)
            return false;

        var items = targetTransitionViewModel.ConditionComponents;
        var sourceIndex = items.IndexOf(sourceComponent);
        var targetIndex = items.IndexOf(targetComponent);
        if (sourceIndex == -1) // If this is -1, then we are moving it to a different node
        {
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
            return Validate<TransitionConditionViewModel>(listBox, e, sourceContext, targetContext, false);
        }

        return false;
    }

    public override bool Execute(object? sender, DragEventArgs e, object? sourceContext, object? targetContext, object? state)
    {
        if (sender is ItemsControl listBox)
        {
            return Validate<TransitionConditionViewModel>(listBox, e, sourceContext, targetContext, true);
        }
        return false;
    }
}
