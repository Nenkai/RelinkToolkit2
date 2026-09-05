using CommunityToolkit.Mvvm.Messaging;

using Dock.Model.Mvvm.Controls;

using RelinkToolkit2.ViewModels.Documents.Interfaces;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RelinkToolkit2.ViewModels.Tools;

public class ToolBase : Tool, IMessageableDock
{
    public bool HasRegistered { get; set; }

    public override bool OnClose()
    {
        OnInactive();
        return base.OnClose();
    }

    /// <summary>
    /// Fired when the document is active - it is ready for use.
    /// </summary>
    public virtual void OnActive()
    {
        if (!HasRegistered)
            WeakReferenceMessenger.Default.RegisterAll(this);

        HasRegistered = true;
    }

    /// <summary>
    /// Fired when the document is inactive, another document has been selected or this one has been closed.
    /// </summary>
    public virtual void OnInactive()
    {
        if (HasRegistered)
            WeakReferenceMessenger.Default.UnregisterAll(this);

        HasRegistered = false;
    }
}
