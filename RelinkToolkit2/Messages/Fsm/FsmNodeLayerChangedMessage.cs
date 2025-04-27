using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RelinkToolkit2.ViewModels.Documents.GraphEditor.Nodes;

using CommunityToolkit.Mvvm.Messaging.Messages;
using Avalonia.Controls;

namespace RelinkToolkit2.Messages.Fsm;

/// <summary>
/// Represents a request for a node change on the graph.
/// </summary>
public class FsmNodeLayerChangedMessage : ValueChangedMessage<NodeViewModelBase>
{
    public FsmNodeLayerChangedMessage(NodeViewModelBase nodeViewModel)
        : base(nodeViewModel)
    {
        
    }
}
