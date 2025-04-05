using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Dock.Model.Mvvm.Controls;

namespace RelinkToolkit2.ViewModels.Documents;

public class EditorDocumentBase : Document, IMessageableDocument
{
    public virtual void RegisterMessageListeners() { }
    public virtual void UnregisterMessageListeners() { }
}
