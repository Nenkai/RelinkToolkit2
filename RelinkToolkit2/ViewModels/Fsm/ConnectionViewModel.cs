using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Collections;
using Avalonia.Media;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using GBFRDataTools.FSM.Entities;

using Nodify;

using RelinkToolkit2.Messages.Fsm;

namespace RelinkToolkit2.ViewModels.Fsm;

/// <summary>
/// Represents a connection on the graph (which can represent start & end connection).
/// </summary>
public partial class ConnectionViewModel : ObservableObject
{
    [ObservableProperty]
    private Point _anchor;

    [ObservableProperty]
    private ArrowHeadEnds _arrowHeadEnds = ArrowHeadEnds.End;

    [ObservableProperty]
    private IBrush _arrowColor = GraphColors.NormalTransition;

    [ObservableProperty]
    private bool _isAnimating = false;

    [ObservableProperty]
    private int _directionalArrowCount = 0;

    [ObservableProperty]
    private string? _title;

    public NodeViewModel Source { get; set; }
    public NodeViewModel Target { get; set; }

    [ObservableProperty]
    private AvaloniaList<double> _strokeDashArray;

    /// <summary>
    /// Transitions. May have up to 2 (source to target and, target to source, if applicable)
    /// </summary>
    public ObservableCollection<TransitionViewModel> Transitions { get; set; } = [];

    public ConnectionViewModel()
    {
        Transitions.CollectionChanged += Transitions_CollectionChanged;
    }

    private void Transitions_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        UpdateConnection();
    }

    public void UpdateConnection()
    {
        if (Transitions.Count == 0)
        {
            Title = string.Empty;
        }
        else if (Transitions.Count == 1)
        {
            if (Transitions[0].ConditionComponents.Count > 0)
            {
                Title = Transitions[0].ConditionComponents.Count > 1 ?
                    $"{Transitions[0].ConditionComponents.Count} conditions" :
                    "1 condition";
            }
            else
                Title = string.Empty;

            ArrowHeadEnds = ArrowHeadEnds.End;
        }
        else if (Transitions.Count == 2)
        {
            ArrowHeadEnds = ArrowHeadEnds.Both;
            Title = "Dual";
        }
    }

    [RelayCommand]
    public void OnTransitionDeleted(TransitionViewModel transition)
    {
        Transitions.Remove(transition);

        if (Transitions.Count == 0)
        {
            WeakReferenceMessenger.Default.Send(new DeleteNodeConnectionRequest(this));
        }
    }
}
