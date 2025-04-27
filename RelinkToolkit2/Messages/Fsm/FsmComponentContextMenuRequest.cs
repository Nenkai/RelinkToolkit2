using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RelinkToolkit2.ViewModels.Documents.GraphEditor;

using CommunityToolkit.Mvvm.Messaging.Messages;
using Avalonia.Controls;

namespace RelinkToolkit2.Messages.Fsm;

/// <summary>
/// Represents a request for a node change on the graph.
/// </summary>
public class FsmComponentContextMenuRequest : RequestMessage<bool>
{
    public Control NodeView { get; set; }
    public NodeComponentViewModel Component { get; }

    public FsmComponentContextMenuRequest(Control nodeView, NodeComponentViewModel nodeComponentVm)
    {
        NodeView = nodeView;
        Component = nodeComponentVm;
    }
}
