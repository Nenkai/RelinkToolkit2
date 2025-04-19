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

namespace RelinkToolkit2.ViewModels.Fsm;

/// <summary>
/// Represents a node on the graph.
/// </summary>
public partial class GroupNodeViewModel : NodeViewModelBase //, IDropTarget
{
    /// <summary>
    /// Guid for this group, mainly for tree view referencing.
    /// </summary>
    [ObservableProperty]
    private Guid _id = Guid.CreateVersion7();

    // Leave this here even though NodeViewModelBase has it.
    // Causes an issue with uh, group sizing.
    [ObservableProperty]
    private Size _size;

    /// <summary>
    /// Nodes in this group/layer.
    /// </summary>
    public ObservableCollection<NodeViewModel> Nodes { get; set; } = [];
}
