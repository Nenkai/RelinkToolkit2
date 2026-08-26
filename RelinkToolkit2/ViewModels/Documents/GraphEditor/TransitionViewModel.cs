using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Nodify;

using RelinkToolkit2.ViewModels.Documents.GraphEditor.Nodes;
using RelinkToolkit2.ViewModels.Documents.GraphEditor.TransitionComponents;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

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

    public ObservableCollection<TransitionConditionOpViewModel> ConditionOperatorGroups { get; set; } = [];

    public TransitionViewModel(FsmConnectionViewModel parentConnection)
    {
        ParentConnection = parentConnection;

        // Default condition group.
        var operatorGroup = new TransitionConditionOpViewModel(parentConnection)
        {
            CanDelete = false,
        };
        ConditionOperatorGroups.Add(operatorGroup);
    }

    [RelayCommand]
    public void OnTransitionComponentDeleted(TransitionConditionOpViewModel component)
    {
        ConditionOperatorGroups.Remove(component);
        ParentConnection.UpdateConnection();
    }
}
