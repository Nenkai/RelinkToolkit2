﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Dock.Model.Mvvm.Controls;

using RelinkToolkit2.ViewModels.Documents.Interfaces;

namespace RelinkToolkit2.ViewModels.Documents;

public class EditorDocumentBase : Document, IMessageableDocument
{
    public TreeViewItemViewModel SolutionTreeViewItem { get; set; }

    public virtual void RegisterMessageListeners() { }
    public virtual void UnregisterMessageListeners() { }

    /// <summary>
    /// Parent document view.
    /// </summary>
    public DocumentsViewModel? Documents { get; set; }

    // Document closing
    public override bool OnClose()
    {
        Documents?.Remove(Id);
        return base.OnClose();
    }
}
