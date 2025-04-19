using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.Messaging.Messages;

namespace RelinkToolkit2.Messages.StatusBar;

public class SetStatusBarTextRequest : RequestMessage<bool>
{
    public string? Text { get; set; }

    public SetStatusBarTextRequest(string? text)
    {
        Text = text;
    }
}
