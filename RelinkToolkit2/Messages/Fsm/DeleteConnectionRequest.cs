
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.Messaging.Messages;

using GBFRDataTools.FSM;

using RelinkToolkit2.ViewModels.Documents.GraphEditor;

namespace RelinkToolkit2.Messages.Fsm;

/// <summary>
/// Represents a request for a node deletion on the graph.
/// </summary>
public class DeleteNodeConnectionRequest : RequestMessage<bool>
{
    public GraphConnectionViewModel Connection { get; set; }

    public DeleteNodeConnectionRequest(GraphConnectionViewModel connection)
    {
        Connection = connection;
    }
}
