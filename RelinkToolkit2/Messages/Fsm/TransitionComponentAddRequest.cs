using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.Messaging.Messages;

using GBFRDataTools.FSM.Components;

namespace RelinkToolkit2.ViewModels.Documents.GraphEditor.TransitionComponents;

/// <summary>
/// Represents a message for when a selected connection has changed.
/// </summary>
public class TransitionComponentAddRequest : RequestMessage<bool>
{
    public required TransitionViewModel Transition { get; set; }
    public required ConditionComponent Component { get; set; }

    public TransitionComponentAddRequest()
    {

    }
}
