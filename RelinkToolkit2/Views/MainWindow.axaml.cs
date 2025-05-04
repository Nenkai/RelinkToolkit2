using Avalonia.Controls;

using CommunityToolkit.Mvvm.Messaging;

using RelinkToolkit2.Messages.Dialogs;

using System.Reflection;

namespace RelinkToolkit2.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        string versionString = Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString(3) ?? "unknown version";
        this.Title += $" (v{versionString})";

        WeakReferenceMessenger.Default.Register<ShowDialogRequest>(this, (recipient, message) =>
        {
            message.Box.ShowWindowDialogAsync(this);
        });
    }
}
