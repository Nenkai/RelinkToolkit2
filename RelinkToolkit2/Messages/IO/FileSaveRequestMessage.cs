using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.Messaging.Messages;

namespace RelinkToolkit2.Messages.IO;

public class FileSaveRequestMessage : ValueChangedMessage<string>
{
    public FileSaveRequestMessage(string value) : base(value)
    {

    }
}
