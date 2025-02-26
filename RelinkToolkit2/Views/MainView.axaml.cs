using Avalonia.Controls;

using RelinkToolkit2.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using RelinkToolkit2.Services;

namespace RelinkToolkit2.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    private void UserControl_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var storageProvider = TopLevel.GetTopLevel(this).StorageProvider;
        App.Current.Services.GetRequiredService<IFilesService>().SetStorageProvider(storageProvider);
    }
}
