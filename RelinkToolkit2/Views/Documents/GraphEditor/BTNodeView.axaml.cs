using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Input;
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

public partial class BTNodeView : UserControl,
    IRecipient<FsmNodeLayerChangedMessage>
{
    private Label? _label;

    public BTNodeView()
    {
        InitializeComponent();
    }

    private CancellationTokenSource? _layerLabelAnimCt = new CancellationTokenSource();
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

        if (_label is not null)
        {
            _layerLabelAnimCt = new CancellationTokenSource();
            var anim = (Animation)_label.Resources["LayerChangedAnimation"]!;
            await anim.RunAsync(_label, cancellationToken: _layerLabelAnimCt.Token);
            _layerLabelAnimCt = null;
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
            }
            else if (properties.IsRightButtonPressed)
            {
                var parentNodeView = control.FindLogicalAncestorOfType<BTNodeView>()!;

                // We gotta pass this to the editor's view to make the context menu.
                WeakReferenceMessenger.Default.Send(new FsmComponentContextMenuRequest(parentNodeView, componentViewModel));
            }

            e.Handled = true;
        }
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

    private void Label_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _label = (Label)sender!;
    }
}