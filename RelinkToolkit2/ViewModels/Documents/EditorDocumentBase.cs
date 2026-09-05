using System.Text.Json.Serialization;

using CommunityToolkit.Mvvm.Messaging;

using Dock.Model.Mvvm.Controls;

using RelinkToolkit2.ViewModels.Documents.Interfaces;
using RelinkToolkit2.ViewModels.TreeView;

namespace RelinkToolkit2.ViewModels.Documents;

public class EditorDocumentBase : Document, IMessageableDock
{
    [JsonIgnore]
    public TreeViewItemViewModel SolutionTreeViewItem { get; set; }

    /// <summary>
    /// Parent document view.
    /// </summary>
    public DocumentsViewModel? Documents { get; set; }

    public bool HasRegistered { get; set; }

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

    public override void OnSelected()
    {
        OnActive();

        base.OnSelected();
    }

    // Document closing
    public override bool OnClose()
    {
        Documents?.Remove(Id);
        return base.OnClose();
    }
}
