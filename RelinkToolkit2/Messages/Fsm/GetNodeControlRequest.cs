﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.Messaging.Messages;
using Avalonia.Controls;

using RelinkToolkit2.ViewModels.Documents.GraphEditor.Nodes;

namespace RelinkToolkit2.Messages.Fsm;

/// <summary>
/// Represents a request for a node change on the graph.
/// </summary>
public class GetNodeControlRequest : RequestMessage<Control>
{
    public NodeViewModel Node { get; }

    public GetNodeControlRequest(NodeViewModel nodeVm)
    {
        Node = nodeVm;
    }
}
