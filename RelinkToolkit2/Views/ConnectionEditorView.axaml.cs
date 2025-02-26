using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Markup.Xaml.Templates;

using Nodify;

using RelinkToolkit2.ViewModels.Fsm;

namespace RelinkToolkit2.Views;

/// <summary>
/// Interaction logic for ConnectorEditorView.xaml
/// </summary>
public partial class ConnectionEditorView : UserControl
{
    public ConnectionEditorView()
    {
        InitializeComponent();
    }

    private void ConnectionExpander_PointerEntered(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (sender is not Control control)
            return;

        var dataContext = control.DataContext;
        if (dataContext is not TransitionViewModel transition)
            return;

        ConnectionViewModel connection = transition.ParentConnection;
        connection.IsAnimating = true;
        connection.DirectionalArrowCount = 4;
        connection.ArrowHeadEnds = Nodify.ArrowHeadEnds.None;
    }

    private void ConnectionExpander_PointerExited(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (sender is not Control control)
            return;

        var dataContext = control.DataContext;
        if (dataContext is not TransitionViewModel transition)
            return;

        ConnectionViewModel connection = transition.ParentConnection;
        connection.IsAnimating = false;
        connection.DirectionalArrowCount = 0;
        connection.ArrowHeadEnds = connection.Transitions.Count == 2 ? Nodify.ArrowHeadEnds.Both : Nodify.ArrowHeadEnds.End;
    }
}
