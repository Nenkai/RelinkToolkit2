using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactions.DragAndDrop;

using RelinkToolkit2.ViewModels.Fsm.TransitionComponents;
using RelinkToolkit2.ViewModels.Fsm;

namespace RelinkToolkit2.Behaviors;

public class TransitionComponentDropHandler : DropHandlerBase
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
        int sourceIndex = items.IndexOf(sourceComponent);
        int targetIndex = items.IndexOf(targetComponent);
        if (sourceIndex == -1) // If this is -1, then we are moving it to a different node
        {
            return true;
        }
        else
        {
            if (bExecute)
            {
                MoveItem(items, sourceIndex, targetIndex);

                // Ensure to adjust the old item since they're between conditions.
                if (sourceIndex > targetIndex)
                    MoveItem(items, targetIndex + 1, sourceIndex);
                else
                    MoveItem(items, targetIndex - 1, sourceIndex);
            }

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
