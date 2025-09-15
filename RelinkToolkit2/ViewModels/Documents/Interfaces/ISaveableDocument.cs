using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Dock.Model.Mvvm.Controls;

using RelinkToolkit2.Services;

namespace RelinkToolkit2.ViewModels.Documents.Interfaces;

/// <summary>
/// Interface for a document that can save.
/// </summary>
public interface ISaveableDocument
{
    public Uri? LastFile { get; set; }
    
    /// <summary>
    /// Whether we should prompt for where to save (first time)
    /// </summary>
    public bool ShouldPromptAsFirstSave { get; set; }

    public Task<Uri?> SaveDocument(IFilesService filesService, bool isSaveAs = false);
}
