using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RelinkToolkit2.ViewModels.Fsm;

using CommunityToolkit.Mvvm.Messaging.Messages;

namespace RelinkToolkit2.Messages.Fsm;

/// <summary>
/// Represents a message for when a selected connection has changed.
/// </summary>
public class ConnectionSelectionChangedMessage : ValueChangedMessage<ConnectionViewModel>
{
    public ConnectionSelectionChangedMessage(ConnectionViewModel? value) : base(value)
    {

    }
}
