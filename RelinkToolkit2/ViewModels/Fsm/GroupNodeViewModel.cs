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
    // Leave this here even though NodeViewModelBase has it.
    // Causes an issue with uh, group sizing.
    [ObservableProperty]
    private Size _size;
}
