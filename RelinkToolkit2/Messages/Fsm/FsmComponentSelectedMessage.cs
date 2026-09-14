using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RelinkToolkit2.ViewModels.Documents.GraphEditor;

using CommunityToolkit.Mvvm.Messaging.Messages;
using GBFRDataTools.FSM.Components;

namespace RelinkToolkit2.Messages.Fsm;

/// <summary>
/// Represents a message for when a node component has been selected on the graph.
/// </summary>
public class FsmComponentSelectedMessage
{
    public BehaviorTreeComponent Component { get; }
    public bool ReadOnly { get; }

    public FsmComponentSelectedMessage(BehaviorTreeComponent component, bool readOnly = false)
    {
        Component = component;
        ReadOnly = readOnly;
    }
}