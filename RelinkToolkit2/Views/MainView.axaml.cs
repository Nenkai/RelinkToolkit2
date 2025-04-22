using Avalonia.Controls;

using RelinkToolkit2.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using RelinkToolkit2.Services;
using Avalonia.Platform.Storage;
using Avalonia.Input;
using System.Collections.Generic;

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
            App.Current.Services.GetRequiredService<IFilesService>().SetStorageProvider(storageProvider);
    }

    private void Drop(object? sender, DragEventArgs e)
    {
        IEnumerable<IStorageItem>? files = e.Data.GetFiles();
        if (files is null)
            return;

        var vm = (MainViewModel)DataContext!;

        foreach (var file in files)
        {
            vm.LoadFile(file.Path);
        }
    }
}
