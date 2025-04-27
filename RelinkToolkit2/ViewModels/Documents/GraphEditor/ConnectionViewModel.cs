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

using RelinkToolkit2.ViewModels.Documents.GraphEditor.Nodes;

namespace RelinkToolkit2.ViewModels.Documents.GraphEditor;

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

    [ObservableProperty]
    private AvaloniaList<double>? _strokeDashArray;

    public GraphConnectionViewModel()
    {
        
    }

    public virtual void UpdateConnection()
    {

    }

    public override string ToString()
    {
        return $"{Source.Guid} -> {Target.Guid}";
    }
}
