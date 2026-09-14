using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactions.DragAndDrop;

using RelinkToolkit2.ViewModels.Documents.GraphEditor;
using RelinkToolkit2.ViewModels.Documents.GraphEditor.TransitionComponents;

using System.Diagnostics;

namespace RelinkToolkit2.Behaviors;

public class TransitionComponentDropHandler : DropHandlerBase
{
    private bool Validate(object? sender, DragEventArgs e, object? sourceContext, TransitionConditionOpViewModel targetOperator, bool bExecute)
    {
        // Moving a group to another group
        if (sourceContext is TransitionConditionOpViewModel sourceGroup)
        {
            if (!sourceGroup.ParentTransition.IsEditable || !targetOperator.ParentTransition.IsEditable)
                return false;

            // Moving a group to the same group (reorder)
            if (sourceGroup.ParentOperand == targetOperator.ParentOperand)
            {
                if (sourceGroup.ParentOperand is null)
                    return false;

                TransitionConditionOpViewModel targetOperatorGroup = sourceGroup.ParentOperand;
                int sourceIndex = targetOperatorGroup.Elements.IndexOf(sourceGroup);
                int targetIndex = targetOperatorGroup.Elements.IndexOf(targetOperator);

                if (sourceIndex != targetIndex)
                {
                    if (bExecute)
                        MoveItem(targetOperatorGroup.Elements, sourceIndex, targetIndex);

                    return true;
                }

                return false;
            }
            else
            {
                ;
            }

            if (bExecute)
            {
                
            }
            return true;
        }
        else if (sourceContext is TransitionConditionViewModel sourceCondition) // Moving a condition to a group
        {
            if (sourceCondition.ParentOperand?.ParentTransition.IsEditable == false)
                return false;

            if (bExecute)
            {
                sourceCondition.ParentOperand?.Elements.Remove(sourceCondition);
                targetOperator.AddElement(sourceCondition);
            }

            return true;
        }

        return false;
    }

    public bool Validate(object? sender, DragEventArgs e, object? sourceContext, TransitionConditionViewModel targetCondition, bool bExecute)
    {
        if (sourceContext == targetCondition)
            return false;

        if (sourceContext is not TransitionConditionElementViewModelBase conditionVmBase)
            return false;

        if (conditionVmBase.ParentOperand?.ParentTransition.IsEditable == false)
            return false;

        if (conditionVmBase.ParentOperand == targetCondition.ParentOperand && conditionVmBase.ParentOperand is not null)
        {
            TransitionConditionOpViewModel targetOperatorGroup = conditionVmBase.ParentOperand;
            int sourceIndex = targetOperatorGroup.Elements.IndexOf(conditionVmBase);
            int targetIndex = targetOperatorGroup.Elements.IndexOf(targetCondition);

            if (!targetOperatorGroup.ParentTransition.IsEditable)
                return false;

            if (sourceIndex != targetIndex)
            {
                if (bExecute)
                    MoveItem(targetOperatorGroup.Elements, sourceIndex, targetIndex);

                return true;
            }
        }

        return true;
    }

    public override bool Validate(object? sender, DragEventArgs e, object? sourceContext, object? targetContext, object? state)
    {
        if (targetContext is TransitionConditionOpViewModel operatorGroup)
            return Validate(sender, e, sourceContext, operatorGroup, false);
        else if (targetContext is TransitionConditionViewModel condition)
            return Validate(sender, e, sourceContext, condition, false);

        return false;
    }

    public override bool Execute(object? sender, DragEventArgs e, object? sourceContext, object? targetContext, object? state)
    {
        if (targetContext is TransitionConditionOpViewModel operatorGroup)
            return Validate(sender, e, sourceContext, operatorGroup, true);
        else if (targetContext is TransitionConditionViewModel condition)
            return Validate(sender, e, sourceContext, condition, true);

        return false;
    }
}
