using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Media;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using GBFRDataTools.FSM.Entities;

namespace RelinkToolkit2.ViewModels.Documents.GraphEditor.Nodes;

/// <summary>
/// Represents a layer group on a FSM graph.
/// </summary>
public partial class FsmGroupNodeViewModel : FsmNodeViewModelBase //, IDropTarget
{
    /// <summary>
    /// Guid for this group, mainly for tree view referencing.
    /// </summary>
    [ObservableProperty]
    private Guid _id = System.Guid.CreateVersion7();

    [ObservableProperty]
    private int _layerIndex;

    [ObservableProperty]
    private Size _size;

    /// <summary>
    /// Nodes in this group/layer.
    /// </summary>
    public ObservableCollection<FsmNodeViewModel> Nodes { get; set; } = [];
}
