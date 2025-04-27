using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using GBFRDataTools.FSM.Components;
using GBFRDataTools.FSM.Components.Actions.AI.Enemy;
using GBFRDataTools.FSM.Components.Actions.Quest;

using Nodify.Compatibility;

using RelinkToolkit2.Messages.Fsm;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RelinkToolkit2.ViewModels.Documents.GraphEditor.Nodes;

/// <summary>
/// Represents a node on a graph.
/// </summary>
public partial class NodeViewModel : NodeViewModelBase //, IDropTarget
{
    [ObservableProperty]
    private Point _anchor;

    [ObservableProperty]
    private IBrush _borderBrush = GraphColors.DefaultNode;

    [ObservableProperty]
    private CornerRadius _cornerRadius = new(3);

    [ObservableProperty]
    private uint _guid;

    /// <summary>
    /// Execution components.
    /// </summary>
    public ObservableCollection<NodeComponentViewModel> Components { get; set; } = [];

    public NodeViewModel()
    {
        
    }
}
