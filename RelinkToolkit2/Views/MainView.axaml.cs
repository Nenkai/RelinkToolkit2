using Avalonia.Controls;

using RelinkToolkit2.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using RelinkToolkit2.Services;
using Avalonia.Platform.Storage;

namespace RelinkToolkit2.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
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
}
