using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform.Storage;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using GBFRDataTools.Entities;
using GBFRDataTools.FSM;
using GBFRDataTools.FSM.BehaviorTree;
using GBFRDataTools.FSM.Components;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using RelinkToolkit2.Messages.Fsm;
using RelinkToolkit2.Services;
using RelinkToolkit2.ViewModels.Documents.GraphEditor.Nodes;
using RelinkToolkit2.ViewModels.Documents.GraphEditor.TransitionComponents;
using RelinkToolkit2.ViewModels.Menu;

namespace RelinkToolkit2.ViewModels.Documents.GraphEditor;

public partial class BTEditorViewModel : EditorDocumentBase, /* ISaveableDocument,*/ IGraphEditor
{
    private ILogger? _logger;

    /// <summary>
    /// Lookup table for all elements with a guid (nodes, or components).
    /// </summary>
    private readonly Dictionary<uint, object> _guidToBtElement = [];

    /// <summary>
    /// All displayed nodes in the graph (including groups, except layer 0).
    /// </summary>
    public ObservableCollection<BTNodeViewModel> Nodes { get; } = [];

    /// <summary>
    /// Connections in the graph.
    /// </summary>
    public ObservableCollection<GraphConnectionViewModel> Connections { get; } = [];

    [ObservableProperty]
    private PendingConnectionViewModel _pendingConnection = new();

    /// <summary>
    /// Currently selected node.
    /// </summary>
    [ObservableProperty]
    private BTNodeViewModel? _selectedNode;

    /// <summary>
    /// Currently selected nodes.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<BTNodeViewModel> _selectedNodes = [];

    /// <summary>
    /// Currently selected connection.
    /// </summary>
    [ObservableProperty]
    private FsmConnectionViewModel? _selectedConnection;

    [ObservableProperty]
    private Point _viewportLocation;

    [ObservableProperty]
    private Size _viewportSize;

    [ObservableProperty]
    private double _viewportZoom = 1.0f;

    [ObservableProperty]
    private Point _contextMenuLocation;

    public Point MouseLocation { get; set; }

    private string? _currentBtName;

    private BTNodeViewModel _rootNode;

    public bool IsLayouted { get; set; } = false;

    /// <summary>
    /// Last name this file was saved as.
    /// </summary>
    public string? LastFile { get; set; }

    /// <summary>
    /// Menu items for the main context menu (add nodes, etc).
    /// </summary>
    public ObservableCollection<MenuItemViewModel> EditorContextMenuItems { get; set; } = [];

    public BTEditorViewModel()
    {
        _logger = App.Current.Services.GetService<ILogger<BTEditorViewModel>>();

        // We are essentially swapping out gestures here.
        // We want group layer click to select the whole layer. not just the group.
        //EditorGestures.Mappings.ItemContainer.Selection.Replace.Value = new MouseGesture(MouseAction.LeftClick, KeyModifiers.Control);
        //EditorGestures.Mappings.ItemContainer.Selection.Invert.Value = new MouseGesture(MouseAction.LeftClick);

        if (Design.IsDesignMode)
        {
            var node = new BTNodeViewModel()
            {
                ParentEditor = this,
                Title = "Test1",
                Guid = 123456789,
                IsLayerRootNode = true,
                Location = new Point(-200, 0),
            };
            node.UpdateBorderColor();
            AddNode(node);

            var node2 = new BTNodeViewModel()
            {
                ParentEditor = this,
                Title = "Test2",
                Guid = 12345678,
                IsEndNode = true,
                Location = new Point(-100, 0),
            };
            node2.UpdateBorderColor();
            AddNode(node2);

            Connections.Add(new GraphConnectionViewModel()
            {
                Source = node,
                Target = node2,
            });

            var selfTransition = new GraphConnectionViewModel()
            {
                Source = node2,
                Target = node2,
            };
            Connections.Add(selfTransition);
        }

        EditorContextMenuItems.Add(new MenuItemViewModel()
        {
            Header = "Editor",
            FontWeight = FontWeight.Bold,
            Enabled = true,
            IsHitTestVisible = false,
        });
        EditorContextMenuItems.Add(MenuItemViewModel.Separator);
        EditorContextMenuItems.Add(new MenuItemViewModel()
        {
            Header = "Add Node",
            IconKind = "Material.PlusBox",
            Enabled = false,
            Command = new RelayCommand(AddNewNode),
        });
    }

    public override void RegisterMessageListeners()
    {
        // Fired when selecting a node manually
        WeakReferenceMessenger.Default.Register<NodeGraphSelectionChangeRequest>(this, (recipient, message) =>
        {
            SelectedNode = (BTNodeViewModel)message.Node;
            message.Reply(true);
        });

        // Fired when deleting a node
        WeakReferenceMessenger.Default.Register<DeleteNodeConnectionRequest>(this, (recipient, message) =>
        {
            GraphConnectionViewModel connection = message.Connection;

            bool result = Connections.Remove(connection);
            message.Reply(result);
        });

        // Fired when a component selection was complete
        WeakReferenceMessenger.Default.Register<TransitionComponentAddRequest>(this, (recipient, message) =>
        {
            ConditionComponent component = message.Component;
            TransitionViewModel transition = message.Transition;

            component.Guid = GetNewGuid();
            RegisterBtElementGuid(component.Guid, component);

            if (transition.ConditionComponents.Count != 0)
                transition.ConditionComponents.Add(new TransitionConditionOpViewModel());

            transition.ConditionComponents.Add(new TransitionConditionViewModel(component) { Title = component.ComponentName });
            transition.ParentConnection.UpdateConnection();
            message.Reply(true);
        });
    }

    public override void UnregisterMessageListeners()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    /// <summary>
    /// Resets graph state.
    /// </summary>
    private void ResetGraph()
    {
        _processedNodes.Clear();
        _guidToBtElement.Clear();

        Nodes.Clear();
        Connections.Clear();

        SelectedNode = null;
        SelectedNodes.Clear();
    }

    /// <summary>
    /// Called by generated observable property <see cref="_selectedNode"/>
    /// </summary>
    /// <param name="value"></param>
    partial void OnSelectedNodeChanged(BTNodeViewModel? value)
    {
        // HACK: This exists because clicking away from a flyout for a node doesn't dismiss the flyout for some reason..
        // Request the view (yikes!) to do so.
        if (value is null)
            WeakReferenceMessenger.Default.Send<DismissSearchComponentMenuRequest>();
    }

    /// <summary>
    /// Saves the graph as a FSM file.
    /// </summary>
    /// <param name="fileName"></param>
    public async Task<string?> SaveDocument(IFilesService filesService, bool isSaveAs = false)
    {
        string? outputPath = LastFile;
        if (isSaveAs || string.IsNullOrEmpty(outputPath))
        {
            var file = await filesService.SaveFileAsync("Save Behavior Tree file", null,
                                  $"{Title}_behavior_tree_ingame.json");
            if (file is null)
                return null;

            outputPath = file.TryGetLocalPath();
        }

        if (string.IsNullOrEmpty(outputPath))
            return null;

        /*
        FSMState buildState = BuildTreeFromCurrentGraph();
        using Stream stream = File.Create(outputPath);
        var builder = new FSMSerializer(buildState);
        builder.WriteJson(stream);
        */

        this.LastFile = outputPath;

        string newTitle = Path.GetFileNameWithoutExtension(outputPath);
        if (newTitle.EndsWith("_behavior_tree_ingame"))
            newTitle = newTitle.Replace("_behavior_tree_ingame", string.Empty);

        _currentBtName = newTitle;
        this.Title = _currentBtName;
        SolutionTreeViewItem.TreeViewName = _currentBtName;

        return outputPath;
    }

    /// <summary>
    /// Inits the graph.
    /// </summary>
    public bool InitGraph(string btName, BTParser? btParser = null)
    {
        _currentBtName = btName;
        ResetGraph();

        if (btParser is not null)
            this.CreateViewModelFromFSMNode(btParser.RootNode);

        return true;
    }



    /// <summary>
    /// Adds a new node to the graph, using the current mouse location.
    /// </summary>
    public void AddNewNode()
    {
        var node = new BTNodeViewModel() { ParentEditor = this };
        node.Guid = GetNewGuid();
        node.Location = MouseLocation;
        node.IsRenaming = true;
        node.Title = "FSMNode";

        AddNode(node);
    }

    /// <summary>
    /// Removes a node from the graph.
    /// </summary>
    /// <param name="nodeVm"></param>
    public void RemoveNode(BTNodeViewModel nodeVm)
    {
        for (int i = Connections.Count - 1; i >= 0; i--)
        {
            GraphConnectionViewModel? connection = Connections[i];
            if (connection.Target == nodeVm || connection.Source == nodeVm)
            {
                Connections.Remove(connection);
            }
        }

        Nodes.Remove(nodeVm);
        UnregisterBtElementGuid(nodeVm.Guid);
    }

    /// <summary>
    /// Removes a connection from the graph.
    /// </summary>
    /// <param name="connection"></param>
    public void RemoveConnection(GraphConnectionViewModel connection)
    {
        Connections.Remove(connection);
    }


    /// <summary>
    /// Creates a graph node from the specified FSM node (model)
    /// </summary>
    /// <param name="node"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    private BTNodeViewModel GetNodeViewModel(TreeNode node, int x, int y, bool loadEditorParameters = false)
    {
        if (_guidToNodeVm.TryGetValue(node.Guid, out BTNodeViewModel? nodeViewModel))
            return nodeViewModel;

        string title;
        if (loadEditorParameters && !string.IsNullOrEmpty(node.Name))
            title = node.Name;
        else
        {
            title = NodeNameStore.TryGetNameForNode(_currentBtName, node.Guid, 0);
            if (string.IsNullOrEmpty(title))
                title = $"{node.Guid}";
        }

        Point location;
        if (loadEditorParameters)
            location = new Point(node.BoundaryBox.X, node.BoundaryBox.Y);
        else
            location = new Point(x, y);

        nodeViewModel = new BTNodeViewModel()
        {
            ParentEditor = this,
            Guid = node.Guid,
            Title = node.GetType().Name,
            Location = location,
        };

        switch (node)
        {
            case RootNode:
                nodeViewModel.BorderBrush = GraphColors.BTRootNode;
                break;

            case DecorationNode decorationNode:
                {
                    nodeViewModel.BorderBrush = GraphColors.BTDecorationNode;
                    string str = $"If {(decorationNode.Param.AnySuccessMode ? "ANY " : "ALL ")}";
                    str += $"is {(decorationNode.Param.IsResultNegated ? "FALSE" : "TRUE")}";
                    if (decorationNode.Param.Once)
                        str += $" (Once)";

                    nodeViewModel.Description = str;
                }
                break;

            case ActionNode actionNode_:
                nodeViewModel.BorderBrush = GraphColors.BTActionNode;

                if (actionNode_.Param.AnySuccessMode)
                    nodeViewModel.Description = $"AnySuccess: {actionNode_.Param.AnySuccessMode}";
                break;

            case ReferenceTreeNode referenceTreeNode:
                nodeViewModel.BorderBrush = GraphColors.DefaultNodeWithComponents;
                nodeViewModel.Description = $"{referenceTreeNode.Param.ReferenceTreeFolderName}/{referenceTreeNode.Param.ReferenceTreeName}\nAssetPattern: {referenceTreeNode.Param.AssetPattern}";
                break;

            case SelectorNode:
                nodeViewModel.BorderBrush = GraphColors.DefaultNode;
                break;

            case FSMNodeForBT fsmNodeForBt:
                nodeViewModel.BorderBrush = Brushes.White;
                nodeViewModel.Description = $"{fsmNodeForBt.Param.FsmFolderName}/{fsmNodeForBt.Param.FsmName}";
                if (fsmNodeForBt.Param.ObjIdList.Count > 0)
                    nodeViewModel.Description += $" - obj ids:\n{string.Join("\n", fsmNodeForBt.Param.ObjIdList.Select(e => (eObjIdType)(e & 0xFFFF0000) + $"{e & 0xFFFF:X4}"))}";
                break;

            case RandomSelectorNode randomSelector:
                nodeViewModel.BorderBrush = Brushes.Yellow;
                int[] chances =
                [
                    randomSelector.Param.ChildNodeRatio0,
                    randomSelector.Param.ChildNodeRatio1,
                    randomSelector.Param.ChildNodeRatio2,
                    randomSelector.Param.ChildNodeRatio3,
                    randomSelector.Param.ChildNodeRatio4,
                    randomSelector.Param.ChildNodeRatio5,
                    randomSelector.Param.ChildNodeRatio6,
                    randomSelector.Param.ChildNodeRatio7,
                ];
                nodeViewModel.Description = $"Weights: [{string.Join(',', chances)}]";
                break;

            default:
                nodeViewModel.BorderBrush = GraphColors.DefaultNode;
                break;
        }

        if (loadEditorParameters)
            nodeViewModel.Size = new Size(node.BoundaryBox.Z, node.BoundaryBox.W);

        if (node is ActionNode actionNode)
        {
            foreach (BehaviorTreeComponent component in actionNode.Actions)
            {
                nodeViewModel.Components.Add(new NodeComponentViewModel(nodeViewModel, component)
                {
                    Name = component.ToString(),
                });
            }
        }
        else if (node is DecorationNode decorationNode)
        {
            foreach (BehaviorTreeComponent component in decorationNode.BehaviorTreeComponent)
            {
                nodeViewModel.Components.Add(new NodeComponentViewModel(nodeViewModel, component)
                {
                    Name = component.ToString(),
                });
            }
        }

        nodeViewModel.UpdateBorderColor();
        _guidToNodeVm.Add(node.Guid, nodeViewModel);

        return nodeViewModel;
    }

    [RelayCommand]
    private void OnConnectionCompleted(NodeViewModel? target)
    {
        if (target is null)
            return;

        if (PendingConnection.Source == PendingConnection.Target)
            return;

        // Bring up context menu
        // Not ideal and not very MVVM friendly to ask the view to respond, but no other way.
        var message = WeakReferenceMessenger.Default.Send(new GetNodeControlRequest(target));

        var sourceNode = (BTNodeViewModel)PendingConnection.Source;
        var targetNode = (BTNodeViewModel)PendingConnection.Target;


        bool canConnect = true;
        bool canOverrideTransition = canConnect && sourceNode.IsLayerRootNode; // Override transitions are only allowed in layer starts
        bool canLayerConnection = targetNode.IsLayerRootNode && !Connections.Any(e => e.Target == targetNode); // connections to root only, make sure nothing else connects

        ObservableCollection<MenuItemViewModel> items =
        [
            new MenuItemViewModel()
            {
                Header = $"{PendingConnection.Source.Title} -> {PendingConnection.Target.Title}",
                FontWeight = FontWeight.Bold,
                IconKind = "Material.SwapHorizontal",
                Enabled = true,
                IsHitTestVisible = false, // Enabled but not greyed out, not clickable
            },
            MenuItemViewModel.Separator,
            new MenuItemViewModel()
            {
                Header = "Transition",
                Enabled = canConnect,
                IconKind = "Material.RayStartArrow",
                IconBrush = GraphColors.NormalTransition,
                Command = new RelayCommand(ConnectNodes),
            },
        ];

        var flyout = new MenuFlyout();
        flyout.ItemsSource = items;
        flyout.ShowAt(message.Response, showAtPointer: true);
    }

    /// <summary>
    /// Applies the current pending connection with the specified connection type.
    /// </summary>
    /// <param name="connectType"></param>
    public void ConnectNodes()
    {
        if (PendingConnection?.Source is null || PendingConnection?.Target is null)
            return;

        GraphConnectionViewModel? existingConnection = Connections.FirstOrDefault(e => e.Source == PendingConnection.Source && e.Target == PendingConnection.Target ||
                                                                                       e.Target == PendingConnection.Source && e.Source == PendingConnection.Target);

        if (existingConnection is null)
        {
            existingConnection = new GraphConnectionViewModel()
            {
                Source = PendingConnection.Source,
                Target = PendingConnection.Target,
            };

            Connections.Add(existingConnection);
        }
    }

    /// <summary>
    /// Creates a node view model for the specified fsm node.
    /// </summary>
    /// <param name="node">FSM file source node</param>
    /// <param name="nodeList">List of all nodes</param>
    /// <param name="depth">Current depth</param>
    /// <returns></returns>
    private BTNodeViewModel? CreateViewModelFromFSMNode(TreeNode node)
    {
        if (_processedNodes.Contains(node.Guid))
            return null;

        _processedNodes.Add(node.Guid);

        BTNodeViewModel thisNodeVm = GetNodeViewModel(node, 0, 0, true);
        AddNode(thisNodeVm);

        if (node is CompositeNode compositeNode)
        {
            foreach (var child in compositeNode.Children)
            {
                BTNodeViewModel? childNodeVm = CreateViewModelFromFSMNode(child);
                if (childNodeVm is not null)
                {
                    var connection = new GraphConnectionViewModel()
                    {
                        Source = thisNodeVm,
                        Target = childNodeVm,
                    };
                    Connections.Add(connection);
                }
            }

        }
        return thisNodeVm;
    }

    private void AddNode(BTNodeViewModel node)
    {
        RegisterBtElementGuid(node.Guid, node);
        Nodes.Add(node);

        foreach (NodeComponentViewModel componentVM in node.Components)
        {
            RegisterBtElementGuid(componentVM.Component.Guid, componentVM.Component);
        }
    }

    /// <summary>
    /// Generates a new guid.
    /// </summary>
    /// <returns></returns>
    public uint GetNewGuid()
    {
        uint guid;
        do
        {
            guid = (uint)Random.Shared.Next();
        } while (_guidToBtElement.ContainsKey(guid));

        return guid;
    }

    /// <summary>
    /// Registers a graph component (node, bt component) to the guid mapping.
    /// </summary>
    /// <param name="guid"></param>
    /// <param name="elem"></param>
    public void RegisterBtElementGuid(uint guid, object elem)
    {
        _guidToBtElement.TryAdd(guid, elem);
    }

    /// <summary>
    /// Unregisters a graph component from the guid mapping.
    /// </summary>
    /// <param name="guid"></param>
    public void UnregisterBtElementGuid(uint guid)
    {
        _guidToBtElement.Remove(guid);
    }

    private readonly Dictionary<uint, BTNodeViewModel> _guidToNodeVm = [];
    private readonly HashSet<uint> _processedNodes = [];
}
