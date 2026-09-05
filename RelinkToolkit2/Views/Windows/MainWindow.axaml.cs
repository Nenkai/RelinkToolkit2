using Avalonia.Controls;

using CommunityToolkit.Mvvm.Messaging;

using RelinkToolkit2.Messages;

using System.Reflection;

namespace RelinkToolkit2.Views.Windows;

public partial class MainWindow : Window,
    IRecipient<SnackbarMessage>
{
    public MainWindow()
    {
        InitializeComponent();

        string versionString = Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString(3) ?? "unknown version";
        Title += $" (v{versionString})";

        WeakReferenceMessenger.Default.RegisterAll(this);
    }

    public void Receive(SnackbarMessage message)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            Snackbar.Show(message.Text, message.Duration);
        });
    }
}
