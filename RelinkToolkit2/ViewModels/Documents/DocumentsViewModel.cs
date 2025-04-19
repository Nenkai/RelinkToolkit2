using CommunityToolkit.Mvvm.Messaging;

using Dock.Model.Controls;
using Dock.Model.Mvvm.Controls;

using RelinkToolkit2.Messages.Documents;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RelinkToolkit2.ViewModels.Documents;

public class DocumentsViewModel : DocumentDock
{
    public Dictionary<string, EditorDocumentBase> _openDocuments = [];
    public EditorDocumentBase? LastDocument { get; private set; }

    public DocumentsViewModel()
    {
        Id = "Documents";
        Title = "Documents";
        CanCreateDocument = true;
        IsCollapsable = false;
        CanCreateDocument = false;
        CanPin = true;
        Proportion = 0.6;

        WeakReferenceMessenger.Default.Register<GetCurrentDocumentRequest>(this, (recipient, message) =>
        {
            message.Reply(LastDocument);
        });
    }

    public void AddDocument(EditorDocumentBase document, bool setActive = true)
    {
        Factory?.AddDockable(this, document);

        if (IsDocumentOpen(document.Id))
        {
            // FIXME: Use proper ids. This is a hack to allow more than the same document for now.
            document.Id += $"_{Random.Shared.Next()}";
            document.Title += "_copy";
        }

        _openDocuments.Add(document.Id, document);

        if (setActive)
        {
            SetActiveDocument(document.Id);
        }
    }

    public void OnNewDocumentOpen(EditorDocumentBase newDocument)
    {
        LastDocument?.UnregisterMessageListeners();
        LastDocument = newDocument;

        newDocument.RegisterMessageListeners();

        WeakReferenceMessenger.Default.Send(new ActiveDocumentChangedMessage(newDocument));
    }

    public void Remove(string id)
    {
        if (_openDocuments.TryGetValue(id, out EditorDocumentBase? doc))
        {
            _openDocuments.Remove(id);
            doc?.UnregisterMessageListeners();

            if (doc is not null && doc == LastDocument)
            {
                LastDocument = null;
                WeakReferenceMessenger.Default.Send(new ActiveDocumentChangedMessage(null));
            }
        }
    }

    public bool SetActiveDocument(string id)
    {
        if (!_openDocuments.TryGetValue(id, out EditorDocumentBase? document))
            return false;

        if (document == LastDocument)
            return true;

        Factory?.SetActiveDockable(document);
        Factory?.SetFocusedDockable(this, document);
        LastDocument = document;
        WeakReferenceMessenger.Default.Send(new ActiveDocumentChangedMessage(document));
        return true;
    }

    public bool IsDocumentOpen(string id)
    {
        return _openDocuments.ContainsKey(id);
    } 
}
