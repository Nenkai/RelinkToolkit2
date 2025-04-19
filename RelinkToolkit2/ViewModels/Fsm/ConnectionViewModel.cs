using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
using RelinkToolkit2.ViewModels.Fsm.TransitionComponents;

namespace RelinkToolkit2.ViewModels.Fsm;

/// <summary>
/// Represents a connection on the graph (which can be bi-directional).
/// </summary>
public partial class GraphConnectionViewModel : ObservableObject
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

    [ObservableProperty]
    private ConnectionDirection? _direction;

    [ObservableProperty]
    public bool _isSelectable = true;

    public required NodeViewModel Source { get; set; }
    public required NodeViewModel Target { get; set; }


    public bool IsLayerConnection => Source.LayerIndex != Target.LayerIndex;

    [ObservableProperty]
    private AvaloniaList<double>? _strokeDashArray;

    /// <summary>
    /// Transitions. May have up to 2 (source to target and, target to source, if applicable)
    /// </summary>
    public ObservableCollection<TransitionViewModel> Transitions { get; set; } = [];

    public GraphConnectionViewModel()
    {
        Transitions.CollectionChanged += Transitions_CollectionChanged;
    }

    public void SetAsLayerConnection()
    {
        StrokeDashArray = [1];
        ArrowHeadEnds = ArrowHeadEnds.None;
        ArrowColor = Brushes.DimGray;
        IsSelectable = false;
    }

    public void SetAnimatingState(bool animating, bool backwards)
    {
        if (Source == Target)
            return;

        IsAnimating = animating;
        if (animating)
        {
            DirectionalArrowCount = 4;
            ArrowHeadEnds = Nodify.ArrowHeadEnds.None;
            Direction = backwards ? ConnectionDirection.Backward : ConnectionDirection.Forward;
        }
        else
        {
            DirectionalArrowCount = 0;
            ArrowHeadEnds = Transitions.Count == 2 ? Nodify.ArrowHeadEnds.Both : Nodify.ArrowHeadEnds.End;
            Direction = ConnectionDirection.Forward;
        }
    }

    private void Transitions_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        UpdateConnection();
    }

    public void UpdateConnection()
    {
        if (Source == Target)
        {
            Title = string.Empty;
            ArrowHeadEnds = ArrowHeadEnds.None;
        }
        else if (Transitions.Count == 0)
        {
            Title = string.Empty;
        }
        else if (Transitions.Count == 1)
        {
            int condCount = Transitions[0].ConditionComponents.Count(e => e is TransitionConditionViewModel);
            if (condCount > 0)
            {
                Title = condCount > 1 ?
                    $"{condCount} conditions" :
                    "1 condition";
            }
            else
                Title = string.Empty;

            if (Transitions[0].Source.LayerIndex == Transitions[0].Target.LayerIndex)
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

    public override string ToString()
    {
        if (Transitions.Count == 2)
            return $"{Source.Guid} <-> {Target.Guid}";
        else
            return $"{Source.Guid} -> {Target.Guid}";
    }
}
