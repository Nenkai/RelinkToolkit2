using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace RelinkToolkit2.Services;

public interface IFilesService
{
    public void SetStorageProvider(IStorageProvider storageProvider);
    public Task<IStorageFile?> OpenFileAsync(string title, string filter);
    public Task<IStorageFile?> SaveFileAsync(string title, string filter, string? suggestedFileName);
}