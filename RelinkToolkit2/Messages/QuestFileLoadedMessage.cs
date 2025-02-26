using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.Messaging.Messages;

namespace RelinkToolkit2.Messages;

/// <summary>
/// Represents a message for when a quest file has been loaded.
/// </summary>
public class QuestFileLoadedMessage : ValueChangedMessage<bool>
{
    public QuestFileLoadedMessage(bool value) : base(value)
    {

    }
}
