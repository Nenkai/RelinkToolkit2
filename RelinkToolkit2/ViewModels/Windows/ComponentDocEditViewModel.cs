using System;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RelinkToolkit2.ViewModels.Windows;

public partial class ComponentDocEditViewModel : ObservableObject
{
    /// <summary>
    /// Lookup key
    /// </summary>
    [ObservableProperty]
    public partial string Key { get; set; }

    /// <summary>
    /// Documentation/Description
    /// </summary>
    [ObservableProperty]
    public partial string? Value { get; set; }

    /// <summary>
    /// Whether the user has saved
    /// </summary>
    public bool HasSaved { get; private set; }

    public Action? CloseRequested { get; set; }

    [RelayCommand]
    public void OnSave()
    {
        HasSaved = true;
        Close();
    }

    [RelayCommand]
    public void OnCancel()
    {
        Close();
    }

    private void Close()
    {
        CloseRequested?.Invoke();
    }
}
