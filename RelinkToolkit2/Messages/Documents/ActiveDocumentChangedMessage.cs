using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.Messaging.Messages;

using RelinkToolkit2.ViewModels.Documents;

namespace RelinkToolkit2.Messages.Documents;

public class ActiveDocumentChangedMessage : ValueChangedMessage<EditorDocumentBase?>
{
    public ActiveDocumentChangedMessage(EditorDocumentBase value) 
        : base(value)
    {
    }
}
