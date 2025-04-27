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

using GBFRDataTools.FSM.Entities;

using Nodify;

using RelinkToolkit2.ViewModels.Documents.GraphEditor.TransitionComponents;
using RelinkToolkit2.ViewModels.Documents.GraphEditor.Nodes;

namespace RelinkToolkit2.ViewModels.Documents.GraphEditor;

/// <summary>
/// Represents a transition between two nodes on the graph (one direction).
/// </summary>
public partial class TransitionViewModel : ObservableObject
{
    public FsmConnectionViewModel ParentConnection { get; set; }

    public required FsmNodeViewModel Source { get; set; }
    public required FsmNodeViewModel Target { get; set; }
    public bool IsOverrideTransition { get; set; }

    public ObservableCollection<TransitionConditionBase> ConditionComponents { get; set; } = [];

    public TransitionViewModel(FsmConnectionViewModel parentConnection)
    {
        ParentConnection = parentConnection;
    }

    [RelayCommand]
    public void OnTransitionComponentDeleted(TransitionConditionViewModel component)
    {
        int idx = ConditionComponents.IndexOf(component);
        if (idx - 1 > 0 && ConditionComponents[idx - 1] is TransitionConditionOpViewModel operatorVm)
            ConditionComponents.Remove(operatorVm);

        ConditionComponents.Remove(component);

        if (ConditionComponents.Count > 0 && ConditionComponents[0] is TransitionConditionOpViewModel firstOp)
            ConditionComponents.Remove(firstOp);

        ParentConnection.UpdateConnection();
    }
}
