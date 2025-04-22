using Avalonia.Platform.Storage;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace RelinkToolkit2.Services;

public interface IFilesService
{
    public void SetStorageProvider(IStorageProvider storageProvider);
    public Task<IStorageFile?> OpenFileAsync(string title, IReadOnlyList<FilePickerFileType>? filters = null);
    public Task<IStorageFile?> SaveFileAsync(string title, IReadOnlyList<FilePickerFileType>? filters = null, string? suggestedFileName = null);
}