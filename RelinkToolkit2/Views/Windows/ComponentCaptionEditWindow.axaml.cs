using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using RelinkToolkit2.ViewModels.Windows;

using System;

namespace RelinkToolkit2.Views.Windows;

public partial class ComponentCaptionEditWindow : Window
{
    public ComponentCaptionEditWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is ComponentCaptionEditViewModel vm)
        {
            vm.CloseRequested = () => Close();
        }
    }
}