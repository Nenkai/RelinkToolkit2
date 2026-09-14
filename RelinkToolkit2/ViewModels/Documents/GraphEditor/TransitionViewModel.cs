
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

using RelinkToolkit2.ViewModels.Documents.GraphEditor.Nodes;
using RelinkToolkit2.ViewModels.Documents.GraphEditor.TransitionComponents;

using static RelinkToolkit2.ViewModels.Documents.GraphEditor.FsmEditorViewModel;

namespace RelinkToolkit2.ViewModels.Documents.GraphEditor;

/// <summary>
/// Represents a transition between two nodes on the graph (one direction).
/// </summary>
public partial class TransitionViewModel : ObservableObject
{
    public FsmConnectionViewModel ParentConnection { get; set; }

    public required FsmNodeViewModel Source { get; set; }
    public required FsmNodeViewModel Target { get; set; }

    /// <summary>
    /// Whether this transition is editable (not locked, i.e from a template FSM)
    /// </summary>
    public bool IsEditable { get; set; } = true;

    [ObservableProperty]
    public partial FsmNodeConnectionType ConnectionType { get; set; }

    public ObservableCollection<TransitionConditionOpViewModel> ConditionOperatorGroups { get; set; } = [];


    private TransitionViewModel()
    {

    }

    public TransitionViewModel(FsmConnectionViewModel parentConnection)
    {
        ParentConnection = parentConnection;

        // Default condition group.
        var operatorGroup = new TransitionConditionOpViewModel(this)
        {
            CanDelete = false,
        };
        ConditionOperatorGroups.Add(operatorGroup);
    }

    [RelayCommand]
    public void OnTransitionComponentDeleted(TransitionConditionOpViewModel component)
    {
        ConditionOperatorGroups.Remove(component);
        ParentConnection.UpdateVisuals();
    }
}
