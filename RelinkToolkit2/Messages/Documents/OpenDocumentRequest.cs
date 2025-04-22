
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.Messaging.Messages;

using Dock.Model.Mvvm.Controls;

using GBFRDataTools.FSM;

using RelinkToolkit2.ViewModels.Documents;

namespace RelinkToolkit2.Messages.Documents;

/// <summary>
/// Requests a document (dock system) to be opened.
/// </summary>
public class OpenDocumentRequest : RequestMessage<bool>
{
    public EditorDocumentBase Document;

    public OpenDocumentRequest(EditorDocumentBase document)
    {
        Document = document;
    }
}
