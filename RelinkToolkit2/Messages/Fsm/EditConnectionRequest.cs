﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.Messaging.Messages;

using RelinkToolkit2.ViewModels.Documents.GraphEditor;

namespace RelinkToolkit2.Messages.Fsm;

/// <summary>
/// Represents a request for a connection to be edited.
/// </summary>
public class EditConnectionRequest : RequestMessage<bool>
{
    public FsmConnectionViewModel Connection { get; set; }

    public EditConnectionRequest(FsmConnectionViewModel value)
    {
        Connection = value;
    }
}
