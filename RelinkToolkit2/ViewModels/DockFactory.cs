using Dock.Avalonia.Controls;
using Dock.Model;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;
/* using Dock.Serializer; */
using Dock.Settings;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NLog.Layouts;

using RelinkToolkit2.ViewModels.Documents;
using RelinkToolkit2.ViewModels.Properties;
using RelinkToolkit2.ViewModels.Tools;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RelinkToolkit2.ViewModels;

public class DockFactory : Factory
{
    public const string LayoutFilePath = "config/dock_layout.json";

    private readonly ILogger _logger;
    private readonly DockState _dockState;

    private IRootDock? _rootDock;
    private readonly DocumentsViewModel _documents;
    private readonly ToolboxViewModel _toolbox;
    private readonly ConnectionEditorViewModel _connectionEditor;
    private readonly SolutionExplorerViewModel _solutionExplorer;
    private ToolDock? _propertiesToolDock;

    public DockFactory(ILogger<DockFactory> logger,
        DockState dockState,

        DocumentsViewModel documents, 
        ToolboxViewModel toolbox, 
        ConnectionEditorViewModel connectionEditor,
        SolutionExplorerViewModel solutionExplorer)
    {
        _logger = logger;
        _dockState = dockState;

        _documents = documents;
        _toolbox = toolbox;
        _connectionEditor = connectionEditor;
        _solutionExplorer = solutionExplorer;
    }

    public IRootDock LoadOrCreateLayout()
    {
        _logger?.LogInformation("Initializing dock layout");

        // TODO: Figure out why serialization doesn't work, what I am doing wrong with the library or MVVM model
        // ^ This extends to App.axaml.cs
        /*
        if (File.Exists(LayoutFilePath))
        {
            _logger?.LogInformation("Loading dock layout from {}", LayoutFilePath);

            try
            {
                using var stream = File.OpenRead(LayoutFilePath);
                IDockSerializer serializer = App.Current.Services.GetRequiredService<IDockSerializer>();
                var deserializedLayout = serializer.Load<IDock?>(stream);

                if (deserializedLayout is IRootDock rootDock)
                {
                    _logger?.LogWarning("Dock layout successfully loaded from {}", LayoutFilePath);

                    _dockState.Restore(deserializedLayout);
                    return rootDock;
                }
                else
                {
                    _logger?.LogWarning("Layout is not root dock, invalid saved layout, creating new one");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load dock layout from {}", LayoutFilePath);
            }
        }
        else
        {
            _logger?.LogInformation("Not loading docklayout from {}, does not exist. Creating new one", LayoutFilePath);
        }
        */

        var layout = CreateLayout();
        return layout;
    }

    public override IRootDock CreateLayout()
    {
        _logger.LogInformation("CreateLayout called, creating new layout");

        var leftSide = CreateLeftSide();
        var rightSideDock = CreateRightSide();

        var mainPane = new ProportionalDock
        {
            Orientation = Orientation.Horizontal,
            IsCollapsable = false,
            ActiveDockable = _documents,
            VisibleDockables = CreateList<IDockable>
            (
                leftSide,
                new ProportionalDockSplitter(),
                _documents, // Refer to DocumentsViewModel.cs for default values such as deck proportion.
                new ProportionalDockSplitter(),
                rightSideDock
            ),
        };

        var rootDock = CreateRootDock();
        rootDock.VisibleDockables = CreateList<IDockable>(mainPane);
        rootDock.ActiveDockable = mainPane;
        rootDock.DefaultDockable = mainPane;
        rootDock.IsCollapsable = false;

        this.ActiveDockableChanged += DockFactory_ActiveDockableChanged;
        _rootDock = rootDock;
        return rootDock;
    }

    private void DockFactory_ActiveDockableChanged(object? sender, Dock.Model.Core.Events.ActiveDockableChangedEventArgs e)
    {
        if (e.Dockable?.Owner is DocumentsViewModel documents && e.Dockable is EditorDocumentBase editorDocument)
        {
            documents.OnNewDocumentOpen(editorDocument);
        }
    }

    public ProportionalDock CreateLeftSide()
    {
        var solutionExplorerToolDock = new ToolDock
        {
            Id = "SolutionExplorer",
            Title = "Solution Explorer",
            IsCollapsable = true,
            GripMode = GripMode.Visible,
            Alignment = Alignment.Left,
            VisibleDockables = CreateList<IDockable>
            (
                _solutionExplorer
            ),
        };

        var connectionEditorToolDock = new ToolDock
        {
            Id = "ConnectionEditor",
            Title = "Connection Editor",
            IsCollapsable = true,
            GripMode = GripMode.Visible,
            Proportion = 0.60,
            Alignment = Alignment.Left,
            VisibleDockables = CreateList<IDockable>
            (
                _connectionEditor
            ),
        };

        var leftPane = new ProportionalDock
        {
            Orientation = Orientation.Vertical,
            IsCollapsable = true,
            ActiveDockable = solutionExplorerToolDock,
            Proportion = 0.20,
            VisibleDockables = CreateList<IDockable>
            (
                solutionExplorerToolDock,
                new ProportionalDockSplitter(),
                connectionEditorToolDock
            ),
        };

        return leftPane;
    }

    public IDockable CreateRightSide()
    {
        _propertiesToolDock = new ToolDock
        {
            Id = "Properties",
            Title = "Properties",
            IsCollapsable = false,
            GripMode = GripMode.Visible,
            Alignment = Alignment.Right,
            VisibleDockables = [],
        };

        return _propertiesToolDock;
    }

    public void RemovePropertyTool(string id)
    {
        var tool = _propertiesToolDock?.VisibleDockables?.FirstOrDefault(e => e.Id == id);
        if (tool is not null)
            CloseDockable(tool);
    }

    /// <summary>
    /// Adds the given tool to the properties pane if it isn't already there and makes it active
    /// </summary>
    public void ShowInPropertiesPane(IDockable dockable, bool makeActive = false)
    {
        if (_propertiesToolDock is null)
            return;

        bool alreadyPresent = _propertiesToolDock.VisibleDockables?.Contains(dockable) == true;
        if (alreadyPresent)
        {
            if (makeActive)
                SetActiveDockable(dockable);
            return;
        }

        if (_propertiesToolDock.VisibleDockables?.Count > 0)
            CloseAllDockables(_propertiesToolDock);

        AddDockable(_propertiesToolDock, dockable);
        if (dockable is ToolBase toolBase)
            toolBase.OnActive();

        if (makeActive)
            SetActiveDockable(dockable);
    }

    public override void InitLayout(IDockable layout)
    {
        DockableLocator = new Dictionary<string, Func<IDockable?>>()
        {
            ["Root"] = () => _rootDock,
            ["Documents"] = () => _documents
        };

        HostWindowLocator = new Dictionary<string, Func<IHostWindow?>>
        {
            [nameof(IDockWindow)] = () => new HostWindow()
        };

        base.InitLayout(layout);
    }
}
