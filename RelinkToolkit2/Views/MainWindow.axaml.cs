using Avalonia.Controls;

using CommunityToolkit.Mvvm.Messaging;

using System.Reflection;

namespace RelinkToolkit2.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        string versionString = Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString(3) ?? "unknown version";
        this.Title += $" (v{versionString})";
    }
}
