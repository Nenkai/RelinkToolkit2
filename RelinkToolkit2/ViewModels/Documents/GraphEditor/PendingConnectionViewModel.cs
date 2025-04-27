using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Media;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using RelinkToolkit2.ViewModels.Documents.GraphEditor.Nodes;

namespace RelinkToolkit2.ViewModels.Documents.GraphEditor;

/// <summary>
/// Represents a connection on the graph (which can represent start & end connection).
/// </summary>
public partial class PendingConnectionViewModel : ObservableObject
{
    [ObservableProperty]
    private NodeViewModel _source;

    [ObservableProperty]
    private NodeViewModel _target;

    [ObservableProperty]
    private bool _isVisible;

    [ObservableProperty]
    private Point _targetLocation;
}
