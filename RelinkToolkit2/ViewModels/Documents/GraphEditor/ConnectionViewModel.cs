using Avalonia;
using Avalonia.Collections;
using Avalonia.Media;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using GBFRDataTools.FSM.Entities;

using Nodify;

using RelinkToolkit2.Messages.Fsm;
using RelinkToolkit2.ViewModels.Documents.GraphEditor.Nodes;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RelinkToolkit2.ViewModels.Documents.GraphEditor;

/// <summary>
/// Represents a connection on the graph (which can be bi-directional).
/// </summary>
public partial class GraphConnectionViewModel : ObservableObject
{
    [ObservableProperty]
    public partial Point Anchor { get; set; }

    [ObservableProperty]
    public partial ArrowHeadEnds ArrowHeadEnds { get; set; } = ArrowHeadEnds.End;

    [ObservableProperty]
    public partial IBrush ArrowColor { get; set; } = GraphColors.NormalTransition;

    [ObservableProperty]
    public partial bool IsAnimating { get; set; } = false;

    [ObservableProperty]
    public partial int DirectionalArrowCount { get; set; } = 0;

    [ObservableProperty]
    public partial string? Title { get; set; }

    [ObservableProperty]
    public partial ConnectionDirection? Direction { get; set; }

    [ObservableProperty]
    public partial bool IsSelectable { get; set; } = true;
    public required NodeViewModel Source { get; set; }
    public required NodeViewModel Target { get; set; }

    [ObservableProperty]
    public partial AvaloniaList<double>? StrokeDashArray { get; set; }

    /// <summary>
    /// Transitions. May have up to 2 (source to target and, target to source, if applicable)
    /// </summary>
    public ObservableCollection<TransitionViewModel> Transitions { get; set; } = [];

    public GraphConnectionViewModel()
    {
        
    }

    [RelayCommand]
    public void OnTransitionDeleted(TransitionViewModel transition)
    {
        bool hadSelfConnection = Transitions.Any(e => e.Source == e.Target);
        if (transition.Source == transition.Target)
            transition.Source.HasSelfTransition = false;

        Transitions.Remove(transition);
        transition.Source.Transitions.Remove(transition);

        if (Transitions.Count == 0 || (hadSelfConnection && !Transitions.Any(e => e.Source == e.Target)))
        {
            WeakReferenceMessenger.Default.Send(new DeleteNodeConnectionRequest(this));
        }
    }

    public virtual void UpdateVisuals()
    {

    }

    public override string ToString()
    {
        if (Transitions.Count == 2)
            return $"{Source.Guid} <-> {Target.Guid}";
        else
            return $"{Source.Guid} -> {Target.Guid}";
    }
}
