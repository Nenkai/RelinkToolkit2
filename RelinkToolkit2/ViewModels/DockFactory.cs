using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Dock.Avalonia.Controls;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;
using RelinkToolkit2.ViewModels.Documents;

namespace RelinkToolkit2.ViewModels;

public class DockFactory : Factory
{
    private IRootDock? _rootDock;
    private readonly DocumentsViewModel _documentsViewModel;
    private readonly ToolboxViewModel _toolboxViewModel;
    private readonly ConnectionEditorViewModel _connectionEditorViewModel;
    private readonly SolutionExplorerViewModel _solutionExplorerViewModel;
    private readonly PropertyGridViewModel _propertyGridViewModel;

    public DockFactory(DocumentsViewModel documentsViewModel, 
        ToolboxViewModel toolboxViewModel, 
        ConnectionEditorViewModel connectionEditorViewModel,
        SolutionExplorerViewModel solutionExplorerViewModel,
        PropertyGridViewModel propertyGridViewModel)
    {
        _documentsViewModel = documentsViewModel;
        _toolboxViewModel = toolboxViewModel;
        _connectionEditorViewModel = connectionEditorViewModel;
        _solutionExplorerViewModel = solutionExplorerViewModel;
        _propertyGridViewModel = propertyGridViewModel;
    }

    public override IRootDock CreateLayout()
    {
        var leftSide = CreateLeftSide();
        var rightSideDock = CreateRightSide();

        var mainPane = new ProportionalDock
        {
            Orientation = Orientation.Horizontal,
            IsCollapsable = false,
            ActiveDockable = _documentsViewModel,
            VisibleDockables = CreateList<IDockable>
            (
                leftSide,
                new ProportionalDockSplitter(),
                _documentsViewModel, // Refer to DocumentsViewModel.cs for default values such as deck proportion.
                new ProportionalDockSplitter(),
                rightSideDock
            ),
        };

        var rootDock = CreateRootDock();
        rootDock.VisibleDockables = CreateList<IDockable>(mainPane);
        rootDock.ActiveDockable = mainPane;
        rootDock.DefaultDockable = mainPane;
        rootDock.IsCollapsable = false;

        _rootDock = rootDock;
        return rootDock;
    }

    public ProportionalDock CreateLeftSide()
    {
        var toolboxDock = new ToolDock
        {
            Id = "Toolbox",
            Title = "Toolbox",
            IsCollapsable = true,
            GripMode = GripMode.Visible,
            Proportion = 0.40,
            VisibleDockables = CreateList<IDockable>
            (
                _toolboxViewModel
            ),
        };

        var connectionEditorToolDock = new ToolDock
        {
            Id = "ConnectionEditor",
            Title = "Connection Editor",
            IsCollapsable = true,
            GripMode = GripMode.Visible,
            Proportion = 0.60,
            VisibleDockables = CreateList<IDockable>
            (
                _connectionEditorViewModel
            ),
        };

        var leftPane = new ProportionalDock
        {
            Orientation = Orientation.Vertical,
            IsCollapsable = true,
            ActiveDockable = toolboxDock,
            Proportion = 0.20,
            VisibleDockables = CreateList<IDockable>
            (
                toolboxDock,
                new ProportionalDockSplitter(),
                connectionEditorToolDock
            ),
        };

        return leftPane;
    }

    public ProportionalDock CreateRightSide()
    {
        var solutionExplorerToolDock = new ToolDock
        {
            Id = "SolutionExplorer",
            Title = "Solution Explorer",
            IsCollapsable = true,
            GripMode = GripMode.Visible,
            VisibleDockables = CreateList<IDockable>
            (
                _solutionExplorerViewModel
            ),
        };

        var propertiesToolDeck = new ToolDock
        {
            Id = "Properties",
            Title = "Properties",
            IsCollapsable = true,
            GripMode = GripMode.Visible,
            VisibleDockables = CreateList<IDockable>
            (
                _propertyGridViewModel
            ),
        };

        var rightPane = new ProportionalDock
        {
            Orientation = Orientation.Vertical,
            IsCollapsable = true,
            ActiveDockable = solutionExplorerToolDock,
            Proportion = 0.20,
            VisibleDockables = CreateList<IDockable>
            (
                solutionExplorerToolDock,
                new ProportionalDockSplitter(),
                propertiesToolDeck
            ),
        };

        return rightPane;
    }

    public override void InitLayout(IDockable layout)
    {
        DockableLocator = new Dictionary<string, Func<IDockable?>>()
        {
            ["Root"] = () => _rootDock,
            ["Documents"] = () => _documentsViewModel
        };

        HostWindowLocator = new Dictionary<string, Func<IHostWindow?>>
        {
            [nameof(IDockWindow)] = () => new HostWindow()
        };

        base.InitLayout(layout);
    }

   
}
