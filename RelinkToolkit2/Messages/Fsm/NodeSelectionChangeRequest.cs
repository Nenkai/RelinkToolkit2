using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RelinkToolkit2.ViewModels.Fsm;

using CommunityToolkit.Mvvm.Messaging.Messages;

namespace RelinkToolkit2.Messages.Fsm;

/// <summary>
/// Represents a request for a node change on the graph.
/// </summary>
public class NodeGraphSelectionChangeRequest : RequestMessage<bool>
{
    public NodeViewModel Node { get; }
    public NodeGraphSelectionChangeRequest(NodeViewModel value)
    {
        Node = value;
    }
}
