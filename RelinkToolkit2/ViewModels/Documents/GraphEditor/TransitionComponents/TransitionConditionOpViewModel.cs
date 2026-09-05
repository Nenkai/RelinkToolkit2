using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Avalonia.Media;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using GBFRDataTools.FSM.Components.Conditions;
using GBFRDataTools.FSM.Entities;

namespace RelinkToolkit2.ViewModels.Documents.GraphEditor.TransitionComponents;

/// <summary>
/// Represents an operator
/// </summary>
public partial class TransitionConditionOpViewModel : TransitionConditionElementViewModelBase
{
    [ObservableProperty]
    private TransitionOperandType? _operand;

    [ObservableProperty]
    public IBrush? _borderBrush;

    [ObservableProperty]
    public bool _canDelete = true;

    /// <summary>
    /// Parent connection where this operand is contained
    /// </summary>
    public FsmConnectionViewModel ParentConnection { get; set; }

    /// <summary>
    /// Use <see cref="AddElement"/> to add any elements.
    /// </summary>
    public ObservableCollection<TransitionConditionElementViewModelBase> Elements { get; set; } = [];

    private TransitionConditionOpViewModel() { }

    public TransitionConditionOpViewModel(FsmConnectionViewModel parentConnection, 
        TransitionConditionOpViewModel? parentOperand = null,
        TransitionOperandType operatorType = TransitionOperandType.None)
    { 
        ParentConnection = parentConnection;
        ParentOperand = parentOperand;
        Operand = operatorType;
    }

    public TransitionConditionOpViewModel AddElement(TransitionConditionElementViewModelBase element)
    {
        element.ParentOperand = this;
        Elements.Add(element);
        return this;
    }

    [RelayCommand]
    public void OnAddOperand()
    {
        AddElement(new TransitionConditionOpViewModel(ParentConnection, this, TransitionOperandType.AND));
    }

    [RelayCommand]
    public void OnDeleteOperand()
    {
        ParentOperand?.Elements.Remove(this);
    }

    partial void OnOperandChanged(TransitionOperandType? value)
    {
        BorderBrush = value switch
        {
            TransitionOperandType.AND => Brushes.YellowGreen,
            TransitionOperandType.OR => Brushes.DeepSkyBlue,
            TransitionOperandType.None or _ => Brushes.Black,
        };
    }

    /// <summary>
    /// Returns a flattened list of components and operators representing the current tree.
    /// </summary>
    /// <param name="components"></param>
    /// <param name="operators"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public void FlattenConditionTree(out List<ConditionComponent> components, out List<Transition.TransitionParam> operators)
    {
        components = [];
        operators = [];

        int maxPriority = GetMaxDepth(this);
        FlattenOperator(this, components, operators, 0);

        int GetMaxDepth(TransitionConditionOpViewModel op)
        {
            int maxDepth = 0;
            foreach (var element in op.Elements)
            {
                if (element is TransitionConditionOpViewModel child)
                    maxDepth = Math.Max(maxDepth, 1 + GetMaxDepth(child));
            }

            return maxDepth;
        }

        void FlattenOperator(TransitionConditionOpViewModel op, List<ConditionComponent> components, List<Transition.TransitionParam> operators, int depth)
        {
            int priority = maxPriority - depth;

            for (int i = 0; i < op.Elements.Count; i++)
            {
                var element = op.Elements[i];
                if (element is TransitionConditionViewModel condition)
                {
                    components.Add(condition.ConditionComponent);
                }
                else if (element is TransitionConditionOpViewModel child)
                {
                    FlattenOperator(child, components, operators, depth + 1);
                }
                else
                {
                    throw new InvalidOperationException($"Unknown condition element type: {element}");
                }

                if (i < op.Elements.Count - 1)
                {
                    operators.Add(new Transition.TransitionParam
                    {
                        IsAndCondition = op.Operand == TransitionOperandType.AND,
                        Priority = priority,
                    });
                }
            }
        }
    }
}

public enum TransitionOperandType
{
    None = -1,
    AND,
    OR,
}
