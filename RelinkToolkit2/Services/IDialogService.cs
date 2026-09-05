// Copyright (c) 2026 Nenkai
// SPDX-License-Identifier: MIT

using Avalonia.Controls;

using MsBox.Avalonia.Base;

using System.Threading.Tasks;

namespace RelinkToolkit2.Services;

public interface IDialogService
{
    void OpenLogWindow();
    void SetMainWindow(Window window);
    Task ShowDialogAsync(object viewModel);
    Task<T?> ShowDialogAsync<T>(object viewModel);
    Task<T> ShowMessageBoxDialogAsync<T>(IMsBox<T> box);
}