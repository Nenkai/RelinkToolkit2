
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.Messaging.Messages;

using GBFRDataTools.FSM;

using RelinkToolkit2.ViewModels.Fsm;

namespace RelinkToolkit2.Messages.Fsm;

public class DeleteNodeConnectionRequest : RequestMessage<bool>
{
    public ConnectionViewModel Connection { get; set; }

    public DeleteNodeConnectionRequest(ConnectionViewModel connection)
    {
        Connection = connection;
    }
}
