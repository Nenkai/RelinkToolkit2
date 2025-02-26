using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

using CommunityToolkit.Mvvm.Messaging;

using Nodify;

using RelinkToolkit2.Messages.Fsm;
using RelinkToolkit2.ViewModels.Fsm;

using System.Diagnostics;

namespace RelinkToolkit2.Views.Fsm;

public partial class FsmNodeView : UserControl
{
    public FsmNodeView()
    {
        InitializeComponent();

        AddHandler(DragDrop.DropEvent, Drop);
        AddHandler(DragDrop.DragOverEvent, DragOver);
    }

    private void Drop(object? sender, DragEventArgs e)
    {
        if (e.Source is Control c && c.DataContext is NodeViewModel targetViewModel)
        {
            if (e.Data.Get("Context") is not NodeComponentViewModel component)
                return;

            if (targetViewModel == component.Parent)
                return;

            component.Parent.Components.Remove(component);
            targetViewModel.Components.Add(component);
            component.Parent = targetViewModel;
        }
    }

    private void DragOver(object? sender, DragEventArgs e)
    {
        if (e.Source is Control c && c.DataContext is NodeViewModel)
        {
            e.DragEffects |= DragDropEffects.Move;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }

    }

    // HACK: We hook both. Why? PointerPressed for some reason only works the first time. Debug window shows it marked as on further uses, but event doesn't fire (??)
    private void Component_Tapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Control control)
            return;

        if (control.DataContext is not NodeComponentViewModel componentViewModel)
            return;

        WeakReferenceMessenger.Default.Send(new FsmComponentSelectedMessage(componentViewModel.Component));
    }

    private void Border_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control)
            return;

        if (control.DataContext is not NodeComponentViewModel componentViewModel)
            return;

        WeakReferenceMessenger.Default.Send(new FsmComponentSelectedMessage(componentViewModel.Component));
    }

    private void Border_PointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is not Control control)
            return;

        if (control.DataContext is not NodeComponentViewModel componentViewModel)
            return;

        componentViewModel.BorderBrush = GraphColors.ComponentBorderHighlight;
    }

    private void Border_PointerExited(object? sender, PointerEventArgs e)
    {
        if (sender is not Control control)
            return;

        if (control.DataContext is not NodeComponentViewModel componentViewModel)
            return;

        componentViewModel.BorderBrush = GraphColors.ComponentBorderNormal;
    }
}