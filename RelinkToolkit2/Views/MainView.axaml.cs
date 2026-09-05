using System.Collections.Generic;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;

using Microsoft.Extensions.DependencyInjection;

using RelinkToolkit2.Services;
using RelinkToolkit2.ViewModels;
using RelinkToolkit2.ViewModels.Windows;

namespace RelinkToolkit2.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();

        AddHandler(DragDrop.DropEvent, Drop);
    }

    private void UserControl_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null || App.Current?.Services is null)
            return;

        IStorageProvider? storageProvider = topLevel.StorageProvider;
        if (storageProvider is not null)
        {
            IFilesService? filesService = App.Current.Services.GetService<IFilesService>();
            if (filesService is not null)
                filesService.SetStorageProvider(storageProvider);
        }
    }

    private void Drop(object? sender, DragEventArgs e)
    {
        IEnumerable<IStorageItem>? files = e.DataTransfer.TryGetFiles();
        if (files is null)
            return;

        var vm = (MainViewModel)DataContext!;

        foreach (var file in files)
        {
            vm.LoadFile(file.Path);
        }
    }
}
