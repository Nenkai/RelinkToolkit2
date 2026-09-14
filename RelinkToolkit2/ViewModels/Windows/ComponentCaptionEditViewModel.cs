using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using GBFRDataTools.FSM.Components;

using SmartFormat;

using System;

namespace RelinkToolkit2.ViewModels.Windows;

public partial class ComponentCaptionEditViewModel : ObservableObject
{
    /// <summary>
    /// Lookup key
    /// </summary>
    [ObservableProperty]
    public required partial string Key { get; set; }

    public required BehaviorTreeComponent Component { get; set; }

    /// <summary>
    /// Documentation/Description
    /// </summary>
    [ObservableProperty]
    public required partial string? Value { get; set; }

    /// <summary>
    /// Documentation/Description
    /// </summary>
    [ObservableProperty]
    public partial string? PreviewString { get; set; }

    /// <summary>
    /// Whether the user has saved
    /// </summary>
    public bool HasSaved { get; private set; }

    public Action? CloseRequested { get; set; }

    partial void OnValueChanged(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            try
            {
                PreviewString = Smart.Format(value, Component);
            }
            catch (Exception ex)
            {
                PreviewString = $"---------- ERROR FORMATTING ----------\n\n{ex.Message}\n\n-------------------------------------------";
            }
        }
    }

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
