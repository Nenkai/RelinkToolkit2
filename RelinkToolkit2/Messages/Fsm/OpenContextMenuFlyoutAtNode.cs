using Avalonia.Controls;

using CommunityToolkit.Mvvm.Messaging.Messages;

using RelinkToolkit2.ViewModels.Documents.GraphEditor.Nodes;
using RelinkToolkit2.ViewModels.Menu;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RelinkToolkit2.Messages.Fsm;

/// <summary>
/// Represents a request for a node change on the graph.
/// </summary>
public class OpenContextMenuFlyoutAtNode : RequestMessage<bool>
{
    public NodeViewModel Node { get; }
    public ObservableCollection<MenuItemViewModel> Items { get; }

    public OpenContextMenuFlyoutAtNode(NodeViewModel nodeVm, ObservableCollection<MenuItemViewModel> items)
    {
        Node = nodeVm;
        Items = items;
    }
}
