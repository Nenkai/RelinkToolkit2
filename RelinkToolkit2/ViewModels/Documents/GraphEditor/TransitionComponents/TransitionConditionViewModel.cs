using Avalonia.PropertyGrid.Controls;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using GBFRDataTools.FSM.Components.Conditions;

using Microsoft.Extensions.DependencyInjection;

using RelinkToolkit2.Controls.PropertyGrid.ViewModel;
using RelinkToolkit2.Messages.Fsm;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace RelinkToolkit2.ViewModels.Documents.GraphEditor.TransitionComponents;

/// <summary>
/// Represents a transition between two nodes on the graph.
/// </summary>
public partial class TransitionConditionViewModel : TransitionConditionElementViewModelBase
{
    [ObservableProperty]
    public partial string? Title { get; set; }

    [ObservableProperty]
    public partial bool IsFalse { get; set; }

    [ObservableProperty]
    public partial bool IsExpanded { get; set; } = false;

    [ObservableProperty]
    public partial ConditionComponent ConditionComponent { get; set; }

    [ObservableProperty]
    public partial DocumentablePropertyGridViewModel PropertyGrid { get; set; }

    private TransitionConditionViewModel() { }

    public TransitionConditionViewModel(ConditionComponent conditionComponent, TransitionConditionOpViewModel? parentOperand = null)
    {
        ArgumentNullException.ThrowIfNull(conditionComponent, nameof(conditionComponent));

        ParentOperand = parentOperand;
        ConditionComponent= conditionComponent;

        PropertyGrid = App.Current.Services.GetRequiredService<DocumentablePropertyGridViewModel>();
        PropertyGrid.SetObject(conditionComponent);
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
            ParentOperand.ParentTransition.ParentConnection.UpdateVisuals();
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
