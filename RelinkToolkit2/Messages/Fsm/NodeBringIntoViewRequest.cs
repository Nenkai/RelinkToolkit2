using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RelinkToolkit2.ViewModels.Fsm;

using CommunityToolkit.Mvvm.Messaging.Messages;

namespace RelinkToolkit2.Messages.Fsm;

/// <summary>
/// Represents a message for when a node should be brought into view.
/// </summary>
public class NodeBringIntoViewRequest : RequestMessage<bool>
{
    public NodeViewModel Node { get; set; }
    public NodeBringIntoViewRequest(NodeViewModel value)
    {
        Node = value;
    }
}
