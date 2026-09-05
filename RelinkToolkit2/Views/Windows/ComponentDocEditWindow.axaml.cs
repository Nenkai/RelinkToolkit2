using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using RelinkToolkit2.ViewModels.Windows;

using System;

namespace RelinkToolkit2.Views.Windows;

public partial class ComponentDocEditWindow : Window
{
    public ComponentDocEditWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is ComponentDocEditViewModel vm)
        {
            vm.CloseRequested = () => Close();
        }
    }
}