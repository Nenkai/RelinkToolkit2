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

using GBFRDataTools.FSM.Components;

using RelinkToolkit2.Messages.Fsm;

namespace RelinkToolkit2.ViewModels.Documents.GraphEditor.TransitionComponents;

/// <summary>
/// Represents a transition between two nodes on the graph.
/// </summary>
public partial class TransitionConditionViewModel : TransitionConditionBase
{
    [ObservableProperty]
    private string? _title;

    [ObservableProperty]
    private bool _isFalse;

    [ObservableProperty]
    private bool _isExpanded = true;

    [ObservableProperty]
    private ConditionComponent _conditionComponent;

    public TransitionConditionViewModel(ConditionComponent conditionComponent)
    {
        ArgumentNullException.ThrowIfNull(conditionComponent, nameof(conditionComponent));

        _conditionComponent = conditionComponent;
    }

    partial void OnIsFalseChanged(bool value)
    {
        ConditionComponent.IsReverseSuccess = value;
    }

    [RelayCommand]
    public void OnEditClicked()
    {
        WeakReferenceMessenger.Default.Send(new FsmComponentSelectedMessage(ConditionComponent));
    }
}
