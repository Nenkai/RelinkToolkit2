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

    // Yes this is VERY bad considering NodeViewModel already has one.
    // That essentially means there's two size properties now, why?
    // Because sizing doesn't seem to work otherwise for some very strange reason
    // Binding related issue, i don't know
    [ObservableProperty]
    private Size _size;

    /// <summary>
    /// Nodes in this group/layer.
    /// </summary>
    public ObservableCollection<FsmNodeViewModel> Nodes { get; set; } = [];
}
