using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

using Avalonia.Controls;
using Avalonia.Markup.Xaml.Templates;

using CommunityToolkit.Mvvm.Messaging;

using RelinkToolkit2.Messages.Fsm;
using RelinkToolkit2.ViewModels;
using RelinkToolkit2.ViewModels.TreeView;

namespace RelinkToolkit2.Views;

/// <summary>
/// Interaction logic for SolutionExplorerView.xaml
/// </summary>
public partial class SolutionExplorerView : UserControl
{
    public SolutionExplorerView()
    {
        InitializeComponent();
    }

    private void StackPanel_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (e.ClickCount == 2 && e.Source is Control control)
        {
            var dataContext = control.DataContext;
            if (dataContext is not TreeViewItemViewModel item)
                return;

            if (item.DoubleClickedCommand?.CanExecute(null) == true)
                item.DoubleClickedCommand?.Execute(null);
        }
    }
}
