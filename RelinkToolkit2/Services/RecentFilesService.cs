using Microsoft.Extensions.Options;

using RelinkToolkit2.Collections;
using RelinkToolkit2.Services.Configuration.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RelinkToolkit2.Services;

public class RecentFilesService
{
    public readonly IOptions<FileHistory> _fileHistory;

    public RecentItemsStack<Uri> RecentItemsStack = new(10);

    public delegate void RecentFilesUpdatedDelegate(RecentItemsStack<Uri> items);

    public event RecentFilesUpdatedDelegate? OnRecentFilesUpdated;

    public RecentFilesService(IOptions<FileHistory> fileHistory)
    {
        _fileHistory = fileHistory;

        foreach (var item in fileHistory.Value.RecentlyOpenedFiles.Reverse<Uri>())
            RecentItemsStack.Push(item);
    }

    public void Add(Uri file)
    {
        RecentItemsStack.Push(file);

        _fileHistory.Value.RecentlyOpenedFiles = RecentItemsStack.ToList();

        OnRecentFilesUpdated?.Invoke(RecentItemsStack);
    }

    public void Remove(Uri file)
    {
        RecentItemsStack.Remove(file);

        _fileHistory.Value.RecentlyOpenedFiles = RecentItemsStack.ToList();

        OnRecentFilesUpdated?.Invoke(RecentItemsStack);
    }

    public IList<Uri> GetRecentlyOpenedFiles()
    {
        return _fileHistory.Value.RecentlyOpenedFiles;
    }
}
