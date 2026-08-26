using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using GBFRDataTools.FSM.Components.Conditions;

using RelinkToolkit2.Messages.Fsm;

namespace RelinkToolkit2.ViewModels.Documents.GraphEditor.TransitionComponents;

/// <summary>
/// Represents a transition between two nodes on the graph.
/// </summary>
public partial class TransitionConditionViewModel : TransitionConditionElementViewModelBase
{
    [ObservableProperty]
    private string? _title;

    [ObservableProperty]
    private bool _isFalse;

    [ObservableProperty]
    private bool _isExpanded = false;

    [ObservableProperty]
    private ConditionComponent _conditionComponent;

    public TransitionConditionViewModel(ConditionComponent conditionComponent, TransitionConditionOpViewModel? parentOperand = null)
    {
        ArgumentNullException.ThrowIfNull(conditionComponent, nameof(conditionComponent));

        ParentOperand = parentOperand;
        _conditionComponent = conditionComponent;
    }

    public static TransitionConditionViewModel FromConditionComponent(ConditionComponent conditionComponent)
    {
        return new TransitionConditionViewModel(conditionComponent)
        {
            Title = conditionComponent.ComponentName,
            IsFalse = conditionComponent.IsReverseSuccess,
        };
    }

    partial void OnIsFalseChanged(bool value)
    {
        ConditionComponent.IsReverseSuccess = value;
    }

    [RelayCommand]
    public void OnRemoveCondition(TransitionConditionViewModel condition)
    {
        if (ParentOperand is not null)
        {
            ParentOperand.Elements.Remove(condition);
            ParentOperand.ParentConnection.UpdateConnection();
        }
    }

    [RelayCommand]
    public void OnEditClicked()
    {
        WeakReferenceMessenger.Default.Send(new FsmComponentSelectedMessage(ConditionComponent));
    }

    public override string ToString()
    {
        return $"{Title} ({ConditionComponent.Guid})";
    }
}
