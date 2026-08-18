using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactions.DragAndDrop;

using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using RelinkToolkit2.Messages.Fsm;
using RelinkToolkit2.ViewModels.Documents.GraphEditor;
using RelinkToolkit2.ViewModels.Documents.GraphEditor.Nodes;

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RelinkToolkit2.Views.Documents.GraphEditor;

public partial class FsmNodeView : UserControl, 
    IRecipient<FsmNodeLayerChangedMessage>
{
    private Label _label;
    private CancellationTokenSource? _layerLabelAnimCt = new();

    public FsmNodeView()
    {
        InitializeComponent();

        AddHandler(DragDrop.DropEvent, Drop);
        AddHandler(DragDrop.DragOverEvent, DragOver);
    }

    private void UserControl_DataContextChanged(object? sender, System.EventArgs e)
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);

        if (DataContext is not null)
        {
            NodeViewModel nvm = (NodeViewModel)DataContext!;
            WeakReferenceMessenger.Default.Register<FsmNodeLayerChangedMessage, uint>(this, nvm.Guid);
        }
    }

    /// <summary>
    /// Fired when the node's layer has changed, where we start an animation.
    /// </summary>
    /// <param name="message"></param>
    public async void Receive(FsmNodeLayerChangedMessage message)
    {
        await AnimateLayerChange();
    }

    private async Task AnimateLayerChange()
    {
        _layerLabelAnimCt?.Cancel();

        _layerLabelAnimCt = new CancellationTokenSource();
        var anim = (Animation)_label!.Resources["LayerChangedAnimation"]!;
        await anim.RunAsync(_label, cancellationToken: _layerLabelAnimCt.Token);
        _layerLabelAnimCt = null;
    }

    private void Drop(object? sender, DragEventArgs e)
    {
        if (e.Source is Control c && c.DataContext is NodeViewModel targetViewModel)
        {
            var component = e.DataTransfer.TryGetManagedValue<NodeComponentViewModel>("Context");
            if (component is null)
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

    private void Border_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control)
            return;

        if (control.DataContext is not NodeComponentViewModel componentViewModel)
            return;

        if (e.Pointer.Type == PointerType.Mouse)
        {
            var properties = e.GetCurrentPoint(this).Properties;
            if (properties.IsLeftButtonPressed)
            {
                WeakReferenceMessenger.Default.Send(new FsmComponentSelectedMessage(componentViewModel.Component));
                e.Handled = true;
            }
            else if (properties.IsRightButtonPressed)
            {
                var parentNodeView = control.FindLogicalAncestorOfType<FsmNodeView>()!;

                // We gotta pass this to the editor's view to make the context menu.
                WeakReferenceMessenger.Default.Send(new FsmComponentContextMenuRequest(parentNodeView, componentViewModel));
                e.Handled = true;
            }
        }
    }

    // HACK: So Notify seems to handle Left click specifically, once a node is clicked, and a component is clicked, it doesn't let
    // other components be pressed so PointerPressed never fires (except for non left clicks).
    // We hook tapped (kind of a hack) for left clicks, seems to work.
    //
    // Ideal solution would probably be to split components into their own view, and then:
    // AddHandler(PointerPressedEvent, Border_Tapped, RoutingStrategies.Tunnel);
    private void Border_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
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

    private void Panel_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Running XAML animation on the Rect control. 
        //animation.RunAsync(this);


        if (e.Pointer.Type == PointerType.Mouse)
        {
            var properties = e.GetCurrentPoint(this).Properties;
            if (!properties.IsLeftButtonPressed)
                return;
        }

        if (sender is not Control control)
            return;

        if (control.DataContext is not FsmNodeViewModel nvm)
            return;

        TransitionViewModel selfTrans = nvm.Transitions.First(e => e.Source == e.Target);
        WeakReferenceMessenger.Default.Send(new EditConnectionRequest(selfTrans.ParentConnection));
    }

    private void Label_Loaded(object? sender, RoutedEventArgs e)
    {
        _label = (Label)sender!;
    }

    private void Component_DeleteClicked(object? sender, PointerPressedEventArgs e)
    {
        if (e.Pointer.Type == PointerType.Mouse)
        {
            var properties = e.GetCurrentPoint(this).Properties;
            if (!properties.IsLeftButtonPressed)
                return;
        }

        if (sender is not Control control)
            return;

        if (control.DataContext is not NodeComponentViewModel nvm)
            return;

        nvm.Parent.Components.Remove(nvm);
        e.Handled = true;
    }

    private void Component_Tapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Control control)
            return;

        if (control.DataContext is not NodeComponentViewModel nvm)
            return;

        nvm.Parent.Components.Remove(nvm);
        e.Handled = true;
    }
}