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

namespace RelinkToolkit2.ViewModels.Fsm;

/// <summary>
/// Represents a transition between two nodes on the graph.
/// </summary>
public partial class TransitionViewModel : ObservableObject
{
    public ConnectionViewModel ParentConnection { get; set; }

    public required NodeViewModel Source { get; set; }
    public required NodeViewModel Target { get; set; }

    public ObservableCollection<TransitionConditionComponentViewModel> ConditionComponents { get; set; } = [];

    public TransitionViewModel(ConnectionViewModel parentConnection)
    {
        ParentConnection = parentConnection;
    }

    [RelayCommand]
    public void OnTransitionComponentDeleted(TransitionConditionComponentViewModel component)
    {
        ConditionComponents.Remove(component);

        ParentConnection.UpdateConnection();
    }
}
