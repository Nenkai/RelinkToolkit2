using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RelinkToolkit2.ViewModels.Fsm;

using CommunityToolkit.Mvvm.Messaging.Messages;
using GBFRDataTools.FSM.Entities;

namespace RelinkToolkit2.Messages.Fsm;

/// <summary>
/// Represents a message for when a node component has been selected on the graph.
/// </summary>
public class FsmComponentSelectedMessage : ValueChangedMessage<BehaviorTreeComponent>
{
    public FsmComponentSelectedMessage(BehaviorTreeComponent component)
        : base(component)
    {

    }
}