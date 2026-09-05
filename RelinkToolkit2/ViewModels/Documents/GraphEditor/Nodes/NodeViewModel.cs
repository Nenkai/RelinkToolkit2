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
public abstract partial class NodeViewModel : NodeViewModelBase
{
    [ObservableProperty]
    public partial Point Anchor { get; set; }

    [ObservableProperty]
    public partial IBrush BackgroundBrush { get; set; } = GraphColors.DefaultNodeBackground;

    [ObservableProperty]
    public partial IBrush BorderBrush { get; set; } = GraphColors.DefaultNodeBorder;

    [ObservableProperty]
    public partial CornerRadius CornerRadius { get; set; } = new(3);

    [ObservableProperty]
    public partial uint Guid { get; set; }

    [ObservableProperty]
    public partial bool GuidVisible { get; set; }

    /// <summary>
    /// Execution components.
    /// </summary>
    public ObservableCollection<NodeComponentViewModel> Components { get; private set; } = [];

    public NodeViewModel()
    {
        Components.CollectionChanged += Components_CollectionChanged;
    }

    private void Components_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        UpdateVisuals();
    }

    public abstract void UpdateVisuals();

    /// <summary>
    /// Deletes a component from the node.
    /// </summary>
    /// <param name="component"></param>
    public void DeleteComponent(NodeComponentViewModel component)
    {
        Components.Remove(component);

        var editor = (FsmEditorViewModel)ParentEditor;
        editor.UnregisterFsmElementGuid(component.Component.Guid);
    }
}
