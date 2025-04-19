using CommunityToolkit.Mvvm.Messaging.Messages;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RelinkToolkit2.Messages.Documents;

/// <summary>
/// Represents a message for when a quest file has been loaded.
/// </summary>
public class DockLayoutResetRequest : RequestMessage<bool>
{
    public DockLayoutResetRequest()
    {

    }
}

