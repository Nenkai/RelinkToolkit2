using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace RelinkToolkit2.Services;

public class FilesService : IFilesService
{
    private IStorageProvider? _storageProvider;

    public FilesService()
    {

    }

    public FilesService(IStorageProvider storageProvider)
    {
        _storageProvider = storageProvider;
    }

    public void SetStorageProvider(IStorageProvider storageProvider)
    {
        _storageProvider = storageProvider;
    }

    public async Task<IStorageFile?> OpenFileAsync(string title, IReadOnlyList<FilePickerFileType>? filters = null)
    {
        if (_storageProvider is null)
            throw new ArgumentNullException("Storage provider is null. It was not initialized.");

        var files = await _storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            Title = title,
            FileTypeFilter = filters,
            AllowMultiple = false
        });

        return files.Count >= 1 ? files[0] : null;
    }

    public async Task<IStorageFile?> SaveFileAsync(string title, IReadOnlyList<FilePickerFileType>? filters = null, string? suggestedFileName = null)
    {
        if (_storageProvider is null)
            throw new ArgumentNullException("Storage provider is null. It was not initialized.");

        return await _storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
        {
            Title = title,
            SuggestedFileName = suggestedFileName,
        });
    }
}