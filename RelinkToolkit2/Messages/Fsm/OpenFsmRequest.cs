
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.Messaging.Messages;

using GBFRDataTools.FSM;

namespace RelinkToolkit2.Messages.Fsm;

public class OpenFsmDocumentRequest : RequestMessage<bool>
{
    public string Id { get; set; }
    public string Name { get; set; }
    public FSMParser FSM { get; set; }

    public OpenFsmDocumentRequest(string id, string name, FSMParser fsm)
    {
        Id = id;
        Name = name;
        FSM = fsm;
    }
}
