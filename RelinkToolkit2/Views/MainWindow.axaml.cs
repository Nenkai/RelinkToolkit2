using Avalonia.Controls;

using CommunityToolkit.Mvvm.Messaging;

using RelinkToolkit2.Messages.Dialogs;

namespace RelinkToolkit2.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        WeakReferenceMessenger.Default.Register<ShowDialogRequest>(this, (recipient, message) =>
        {
            message.Box.ShowWindowDialogAsync(this);
        });
    }
}
