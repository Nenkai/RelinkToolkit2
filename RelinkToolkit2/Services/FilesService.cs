using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

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

    public async Task<IStorageFile?> SaveFileAsync(string title, IReadOnlyList<FilePickerFileType>? filters = null, string? suggestedFileName = null, Uri? suggestedStartLocation = null)
    {
        if (_storageProvider is null)
            throw new ArgumentNullException("Storage provider is null. It was not initialized.");

        IStorageFolder? startLocation = null;
        if (suggestedStartLocation is not null)
        {
            startLocation = await _storageProvider.TryGetFolderFromPathAsync(suggestedStartLocation);
            startLocation ??= await _storageProvider.TryGetFolderFromPathAsync(new Uri(suggestedStartLocation, ".")); // Try parent
        }

        return await _storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
        {
            Title = title,
            SuggestedFileName = suggestedFileName,
            SuggestedStartLocation = startLocation,
        });
    }

    public async Task<IStorageFolder?> OpenFolderAsync(string title, string? suggestedFileName = null, Uri? suggestedStartLocation = null)
    {
        if (_storageProvider is null)
            throw new ArgumentNullException("Storage provider is null. It was not initialized.");

        IStorageFolder? startLocation = null;
        if (suggestedStartLocation is not null)
        {
            startLocation = await _storageProvider.TryGetFolderFromPathAsync(suggestedStartLocation);
            startLocation ??= await _storageProvider.TryGetFolderFromPathAsync(new Uri(suggestedStartLocation, ".")); // Try parent
        }

        var folder = await _storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
        {
            Title = title,
            SuggestedFileName = suggestedFileName,
            SuggestedStartLocation = startLocation,
        });

        return folder.Count >= 1 ? folder[0] : null;
    }
}