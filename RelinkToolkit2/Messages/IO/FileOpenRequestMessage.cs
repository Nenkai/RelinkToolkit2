using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.Messaging.Messages;

namespace RelinkToolkit2.Messages.IO;

public class FileOpenRequestMessage : ValueChangedMessage<FileOpenResult>
{
    public FileOpenRequestMessage(FileOpenResult value) : base(value)
    {

    }
}

public record FileOpenResult(Uri Uri, Stream Stream);
