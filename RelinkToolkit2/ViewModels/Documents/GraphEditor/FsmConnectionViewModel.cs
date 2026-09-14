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
using RelinkToolkit2.ViewModels.Documents.GraphEditor.TransitionComponents;

namespace RelinkToolkit2.ViewModels.Documents.GraphEditor;

/// <summary>
/// Represents a connection on the graph (which can be bi-directional).
/// </summary>
public partial class FsmConnectionViewModel : GraphConnectionViewModel
{
    public FsmConnectionViewModel()
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
        UpdateVisuals();
    }

    public override void UpdateVisuals()
    {

        ArrowHeadEnds = ArrowHeadEnds.None;
        if (Transitions.Count == 0 || Transitions.All(e => e is null))
        {
            Title = string.Empty;
            return;
        }

        string title = "";
        bool hasBothWays = Transitions.Any(e => e.Source == Target) && Transitions.Any(e => e.Target == Source);

        // Same Layer?
        if (Transitions[0].Source.LayerIndex == Transitions[0].Target.LayerIndex)
        {
            ArrowHeadEnds = ArrowHeadEnds.End;

            // Both Directions?
            if (hasBothWays)
            {
                title += "Dual";
                ArrowHeadEnds = ArrowHeadEnds.Both;
            }
        }

        bool hasConditions = Transitions.Any(trans => trans.ConditionOperatorGroups.Any(e => e.Elements.Count > 0));
        if (hasConditions)
        {
            if (!string.IsNullOrWhiteSpace(title))
                title += " + ";

            title += "Conditions";
        }

        // More than one transition from a node to another, same direction? (i.e, normal + override)
        if (Transitions.Count(e => e.Source == Source) >= 2 || Transitions.Count(e => e.Target == Target) >= 2)
        {
            if (!string.IsNullOrWhiteSpace(title))
                title += " + ";

            title += "Multiple";
        }

        Title = title;
    }
}
