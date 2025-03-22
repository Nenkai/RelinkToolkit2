using Dock.Model.Controls;
using Dock.Model.Mvvm.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RelinkToolkit2.ViewModels.Documents;

public class DocumentsViewModel : DocumentDock
{
    public Dictionary<string, IDocument> _openDocuments = [];

    public DocumentsViewModel()
    {
        Id = "Documents";
        Title = "Documents";
        CanCreateDocument = true;
        IsCollapsable = false;
        CanCreateDocument = false;
        CanPin = true;
        Proportion = 0.6;
    }

    public void AddDocument(IDocument document, bool setActive = true)
    {
        Factory?.AddDockable(this, document);

        if (setActive)
        {
            Factory?.SetActiveDockable(document);
            Factory?.SetFocusedDockable(this, document);
        }

        if (!IsDocumentOpen(document.Id))
            _openDocuments.Add(document.Id, document);
        else
        {
            // FIXME: Use proper ids. This is a hack to allow more than the same document for now.
            document.Id += $"_{Random.Shared.Next()}";
            document.Title += "_copy";

            _openDocuments.Add(document.Id, document);
        }
    }

    public void Remove(string id)
    {
        _openDocuments.Remove(id);
    }

    public bool SetActiveDocument(string id)
    {
        if (!_openDocuments.TryGetValue(id, out IDocument? document))
            return false;

        Factory?.SetActiveDockable(document);
        Factory?.SetFocusedDockable(this, document);
        return true;
    }

    public bool IsDocumentOpen(string id)
    {
        return _openDocuments.ContainsKey(id);
    } 
}
