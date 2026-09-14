// Copyright (c) 2026 Nenkai
// SPDX-License-Identifier: MIT

using Avalonia.Controls;

using MsBox.Avalonia.Base;

using RelinkToolkit2.ViewModels;
using RelinkToolkit2.ViewModels.Windows;
using RelinkToolkit2.Views.Windows;

using System;
using System.Threading.Tasks;

namespace RelinkToolkit2.Services;

public class DialogService : IDialogService
{
    private Window _mainWindow;

    public void SetMainWindow(Window window)
        => _mainWindow = window;

    public async Task ShowDialogAsync(object viewModel)
    {
        ArgumentNullException.ThrowIfNull(_mainWindow);

        var window = ResolveWindow(viewModel);
        window.DataContext = viewModel;
        await window.ShowDialog(_mainWindow);
    }

    public async Task<T?> ShowDialogAsync<T>(object viewModel)
    {
        ArgumentNullException.ThrowIfNull(_mainWindow);

        var window = ResolveWindow(viewModel);
        window.DataContext = viewModel;
        return await window.ShowDialog<T?>(_mainWindow);
    }

    private static Window ResolveWindow(object viewModel) => viewModel switch
    {
        StringHasherViewModel => new StringHasherWindow(),
        ComponentDocEditViewModel => new ComponentDocEditWindow(),
        ComponentCaptionEditViewModel => new ComponentCaptionEditWindow(),
        _ => throw new ArgumentException($"No window registered for {viewModel.GetType().Name}")
    };

    public void OpenLogWindow()
    {
        var logs = new LogWindow();
        logs.Show();
    }

    public async Task<T> ShowMessageBoxDialogAsync<T>(IMsBox<T> box)
    {
        return await box.ShowWindowDialogAsync(_mainWindow);
    }
}