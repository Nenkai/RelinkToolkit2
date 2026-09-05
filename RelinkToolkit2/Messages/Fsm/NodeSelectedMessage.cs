using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RelinkToolkit2.ViewModels.Documents.GraphEditor.Nodes;

namespace RelinkToolkit2.Messages.Fsm;

public class NodeSelectedMessage
{
    public NodeViewModel? Node { get; }

    public NodeSelectedMessage(NodeViewModel? node)
    {
        Node = node;
    }
}
