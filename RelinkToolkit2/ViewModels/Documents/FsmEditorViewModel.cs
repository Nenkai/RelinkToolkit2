
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform.Storage;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using GBFRDataTools.FSM;
using GBFRDataTools.FSM.Components;
using GBFRDataTools.FSM.Entities;
using GBFRDataTools.Hashing;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using MsBox.Avalonia;

using Nodify;
using Nodify.Compatibility;

using RelinkToolkit2.Messages.Dialogs;
using RelinkToolkit2.Messages.Fsm;
using RelinkToolkit2.Services;
using RelinkToolkit2.ViewModels.Documents.Interfaces;
using RelinkToolkit2.ViewModels.Fsm;
using RelinkToolkit2.ViewModels.Fsm.TransitionComponents;
using RelinkToolkit2.ViewModels.Menu;
using RelinkToolkit2.ViewModels.TreeView;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace RelinkToolkit2.ViewModels.Documents;

public partial class FsmEditorViewModel : EditorDocumentBase, ISaveableDocument
{
    private ILogger? _logger;

    /// <summary>
    /// Lookup table for all elements with a guid (nodes, or components).
    /// </summary>
    private readonly Dictionary<uint, object> _guidToFsmElement = [];

    /// <summary>
    /// All displayed nodes in the graph (including groups, except layer 0).
    /// </summary>
    public ObservableCollection<NodeViewModelBase> Nodes { get; } = [];

    /// <summary>
    /// Connections in the graph.
    /// </summary>
    public ObservableCollection<GraphConnectionViewModel> Connections { get; } = [];

    /// <summary>
    /// All layers (as a group) on the graph.<br/>
    /// The root layer (layer 0) is included.
    /// </summary>
    public Dictionary<int, GroupNodeViewModel> LayerGroups { get; set; } = [];

    [ObservableProperty]
    private PendingConnectionViewModel _pendingConnection = new();

    /// <summary>
    /// Currently selected node.
    /// </summary>
    [ObservableProperty]
    private NodeViewModel? _selectedNode;

    /// <summary>
    /// Currently selected nodes.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<NodeViewModelBase> _selectedNodes = [];

    /// <summary>
    /// Currently selected connection.
    /// </summary>
    [ObservableProperty]
    private GraphConnectionViewModel? _selectedConnection;

    [ObservableProperty]
    private Point _viewportLocation;

    [ObservableProperty]
    private Size _viewportSize;

    [ObservableProperty]
    private double _viewportZoom = 1.0f;

    [ObservableProperty]
    private Point _contextMenuLocation;

    public Point MouseLocation { get; set; }

    private string? _currentFsmName;

    private NodeViewModel _rootNode;

    public bool IsLayouted { get; set; } = false;

    /// <summary>
    /// Last name this file was saved as.
    /// </summary>
    public string? LastFile { get; set; }

    /// <summary>
    /// Menu items for the main context menu (add nodes, etc).
    /// </summary>
    public ObservableCollection<MenuItemViewModel> EditorContextMenuItems { get; set; } = [];
    public MenuItemViewModel AddLayerMenuItem { get; set; }

    public FsmEditorViewModel()
    {
        _logger = App.Current.Services.GetService<ILogger<FsmEditorViewModel>>();

        // We are essentially swapping out gestures here.
        // We want group layer click to select the whole layer. not just the group.
        //EditorGestures.Mappings.ItemContainer.Selection.Replace.Value = new MouseGesture(MouseAction.LeftClick, KeyModifiers.Control);
        //EditorGestures.Mappings.ItemContainer.Selection.Invert.Value = new MouseGesture(MouseAction.LeftClick);

        var rootLayer = new GroupNodeViewModel() { ParentEditor = this, LayerIndex = 0 };
        LayerGroups.Add(0, rootLayer);

        if (Design.IsDesignMode)
        {
            var node = new NodeViewModel()
            {
                ParentEditor = this,
                Title = "Test1",
                Guid = 123456789,
                FsmSource = "playerai/test",
                IsLayerRootNode = true,
                Location = new Point(-200, 0),
                ParentGroup = rootLayer,
            };
            node.UpdateBorderColor();
            AddNode(node);

            var node2 = new NodeViewModel()
            {
                ParentEditor = this,
                Title = "Test2",
                Guid = 12345678,
                FsmSource = "playerai/test",
                IsEndNode = true,
                Location = new Point(-100, 0),
                ParentGroup = rootLayer,
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
            selfTransition.Transitions.Add(new TransitionViewModel(selfTransition)
            {
                Source = node2,
                Target = node2,
            });
            Connections.Add(selfTransition);

            var layerGroup = new GroupNodeViewModel()
            {
                ParentEditor = this,
                Title = "Layer 1",
                LayerIndex = 1,
                Location = new Point(100, 150),
            };
            LayerGroups.Add(layerGroup.LayerIndex, layerGroup);
            Nodes.Add(layerGroup);
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
            Enabled = true,
            Command = new RelayCommand(AddNewNode),
        });
        AddLayerMenuItem = new MenuItemViewModel()
        {
            Header = "Add Layer",
            IconKind = "Material.LayersPlus",
            Enabled = true,
            Command = new RelayCommand(AddNewLayer),
        };
        EditorContextMenuItems.Add(AddLayerMenuItem);
    }

    public override void RegisterMessageListeners()
    {
        // Fired when selecting a node manually
        WeakReferenceMessenger.Default.Register<NodeGraphSelectionChangeRequest>(this, (recipient, message) =>
        {
            SelectedNode = message.Node;
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
            RegisterFsmElementGuid(component.Guid, component);

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
        _guidToFsmElement.Clear();

        Nodes.Clear();
        Connections.Clear();
        LayerGroups.Clear();

        SelectedNode = null;
        SelectedNodes.Clear();

        var rootLayer = new GroupNodeViewModel() { ParentEditor = this, LayerIndex = 0 };
        LayerGroups.Add(0, rootLayer);
    }

    /// <summary>
    /// Called by generated observable property <see cref="_selectedNode"/>
    /// </summary>
    /// <param name="value"></param>
    partial void OnSelectedNodeChanged(NodeViewModel? value)
    {
        // HACK: This exists because clicking away from a flyout for a node doesn't dismiss the flyout for some reason..
        // Request the view (yikes!) to do so.
        if (value is null)
            WeakReferenceMessenger.Default.Send<DismissSearchComponentMenuRequest>();
    }

    /// <summary>
    /// Caleld by generated observable property <see cref="_selectedConnection"/>
    /// </summary>
    /// <param name="value"></param>
    partial void OnSelectedConnectionChanged(GraphConnectionViewModel? value)
    {
        if (value?.IsLayerConnection == true)
            return;

        WeakReferenceMessenger.Default.Send(new EditConnectionRequest(value));
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
            var file = await filesService.SaveFileAsync("Save FSM file", null,
                                  $"{Title}_fsm_ingame.json");
            if (file is null)
                return null;

            outputPath = file.TryGetLocalPath();
        }

        if (string.IsNullOrEmpty(outputPath))
            return null;

        FSMState buildState = BuildTreeFromCurrentGraph();
        using Stream stream = File.Create(outputPath);
        var builder = new FSMSerializer(buildState);
        builder.WriteJson(stream);

        this.LastFile = outputPath;

        string newTitle = Path.GetFileNameWithoutExtension(outputPath);
        if (newTitle.EndsWith("_fsm_ingame"))
            newTitle = newTitle.Replace("_fsm_ingame", string.Empty);

        _currentFsmName = newTitle;
        this.Title = _currentFsmName;
        SolutionTreeViewItem.TreeViewName = _currentFsmName;

        return outputPath;
    }

    /// <summary>
    /// Inits the graph from the specified parser.
    /// </summary>
    public bool InitGraph(string fsmName, FSMParser fsmParser)
    {
        _currentFsmName = fsmName;
        ResetGraph();

        try
        {
            if (fsmParser?.RootNode is null)
            {
                foreach (var group in fsmParser.NonEmptyLayersToNodes)
                {
                    if (group.Count == 0)
                        continue;

                    int depth = 0;
                    CreateViewModelFromFSMNode(group[0], fsmParser.AllNodes, ref depth, fsmParser.EditorSettings is not null);
                }
            }
            else
            {
                int depth = 0;
                NodeViewModel? root = CreateViewModelFromFSMNode(fsmParser.RootNode, fsmParser.AllNodes, ref depth, fsmParser.EditorSettings is not null);

                // Reference nodes for each layer
                foreach (NodeViewModelBase node in Nodes)
                {
                    if (node is NodeViewModel nodeViewModel)
                    {
                        var group = LayerGroups[node.LayerIndex];
                        group.Nodes.Add(nodeViewModel);
                        nodeViewModel.ParentGroup = group;
                    }
                }

                if (root is not null)
                {
                    root.SetRootNodeState(true);
                    _rootNode = root;
                }
            }
        }
        catch (Exception ex)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", $"Failed to setup the graph for the FSM.\n{ex.Message}", icon: MsBox.Avalonia.Enums.Icon.Error);
            WeakReferenceMessenger.Default.Send(new ShowDialogRequest(box));
            return false;
        }

        if (fsmParser.EditorSettings is not null)
        {
            IsLayouted = true;
            /*
            ViewportLocation = new Point(fsmParser.EditorSettings.ViewportLocation.X, fsmParser.EditorSettings.ViewportLocation.Y);
            ViewportSize = new Size(fsmParser.EditorSettings.ViewportSize.X, fsmParser.EditorSettings.ViewportSize.Y);
            ViewportZoom = fsmParser.EditorSettings.ViewportZoom;
            */
        }

        return true;
    }

    /// <summary>
    /// Adds a new node to the graph, using the current mouse location.
    /// </summary>
    public void AddNewNode()
    {
        var node = new NodeViewModel() { ParentEditor = this };
        node.Guid = GetNewGuid();
        node.Location = MouseLocation;
        node.IsRenaming = true;
        node.Title = "FSMNode";
        node.ParentGroup = LayerGroups[0];

        AddNode(node);
        UpdateNodeLayerFromLocation(node);
    }

    /// <summary>
    /// Adds a new node to the graph, using the current mouse location on the graph.
    /// </summary>
    public void AddNewLayer()
    {
        int layerIndex = this.LayerGroups.MaxBy(e => e.Key).Key + 1;

        var layerGroup = new GroupNodeViewModel() 
        { 
            ParentEditor = this, 
            LayerIndex = layerIndex, 
            Location = MouseLocation,
            Title = $"Layer {layerIndex}",
            IsRenaming = true,
        };

        LayerGroups.Add(layerGroup.LayerIndex, layerGroup);
        Nodes.Add(layerGroup);

        SolutionExplorerViewModel? solutionExplorerVM = App.Current?.Services?.GetRequiredService<SolutionExplorerViewModel>();
        solutionExplorerVM?.AddItem(new FSMLayerTreeViewItemViewModel()
        {
            LayerGroup = layerGroup,
            TreeViewName = layerGroup.Title,
            Guid = layerGroup.Id,
            Caption = $"Layer {layerGroup.LayerIndex}",
        }, SolutionTreeViewItem.Guid);
    }

    /// <summary>
    /// Removes a node from the graph.
    /// </summary>
    /// <param name="nodeVm"></param>
    public void RemoveNode(NodeViewModel nodeVm)
    {
        for (int i = Connections.Count - 1; i >= 0; i--)
        {
            GraphConnectionViewModel? connection = Connections[i];
            if (connection.Target == nodeVm || connection.Source == nodeVm)
            {
                Connections.Remove(connection);
                RemoveAllTransitionsOfNodeToTargetNode(connection.Source, nodeVm.Guid);
                RemoveAllTransitionsOfNodeToTargetNode(connection.Target, nodeVm.Guid);
            }
        }

        Nodes.Remove(nodeVm);
        UnregisterFsmElementGuid(nodeVm.Guid);
    }

    /// <summary>
    /// Removes a layer from the graph.
    /// </summary>
    /// <param name="layerGroup"></param>
    public void RemoveLayer(GroupNodeViewModel layerGroup)
    {
        foreach (var node in layerGroup.Nodes)
            RemoveNode(node);

        Nodes.Remove(layerGroup);
        LayerGroups.Remove(layerGroup.LayerIndex);

        var solutionExplorerVM = App.Current!.Services!.GetRequiredService<SolutionExplorerViewModel>();
        solutionExplorerVM.RemoveItem(layerGroup.Id);
    }

    /// <summary>
    /// Removes a connection from the graph.
    /// </summary>
    /// <param name="connection"></param>
    public void RemoveConnection(GraphConnectionViewModel connection)
    {
        RemoveAllTransitionsOfNodeToTargetNode(connection.Source, connection.Source.Guid);
        RemoveAllTransitionsOfNodeToTargetNode(connection.Target, connection.Target.Guid);

        Connections.Remove(connection);
    }

    /// <summary>
    /// Removes all transitions to the specified node.
    /// </summary>
    /// <param name="nodeVM"></param>
    /// <param name="guid"></param>
    private void RemoveAllTransitionsOfNodeToTargetNode(NodeViewModel nodeVM, uint guid)
    {
        for (int i = nodeVM.Transitions.Count - 1; i >= 0; i--)
        {
            TransitionViewModel transition = nodeVM.Transitions[i];
            if (transition.Source.Guid == guid || transition.Target.Guid == guid)
            {
                foreach (TransitionConditionBase component in transition.ConditionComponents)
                {
                    if (component is TransitionConditionViewModel condVM)
                        UnregisterFsmElementGuid(condVM.ConditionComponent.Guid);
                }
                nodeVM.Transitions.Remove(transition);
            }
        }
    }

    /// <summary>
    /// Creates a graph node from the specified FSM node (model)
    /// </summary>
    /// <param name="node"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    private NodeViewModel GetNodeViewModel(FSMNode node, int x, int y, bool loadEditorParameters = false)
    {
        if (_guidToNodeVm.TryGetValue(node.Guid, out NodeViewModel? nodeViewModel))
            return nodeViewModel;

        string title;
        if (loadEditorParameters && !string.IsNullOrEmpty(node.Name))
            title = node.Name;
        else
        {
            title = NodeNameStore.TryGetNameForNode(_currentFsmName, node.Guid, node.NameHash);
            if (string.IsNullOrEmpty(title))
                title = $"{node.Guid}";
        }

        Point location;
        if (loadEditorParameters)
            location = new Point(node.BoundaryBox.X, node.BoundaryBox.Y);
        else
            location = new Point(x, y);

        nodeViewModel = new NodeViewModel()
        {
            ParentEditor = this,
            Guid = node.Guid,
            Title = title,
            Location = location,
            LayerIndex = node.LayerIndex,
            NameHash = node.NameHash,
            FsmFolderName = node.FsmFolderName,
            FsmName = node.FsmName,
        };

        if (loadEditorParameters)
            nodeViewModel.Size = new Size(node.BoundaryBox.Z, node.BoundaryBox.W);

        if (!string.IsNullOrEmpty(node.FsmFolderName))
            nodeViewModel.SetBaseFsm(node.FsmFolderName, node.FsmName);

        foreach (BehaviorTreeComponent component in node.ExecutionComponents)
        {
            nodeViewModel.Components.Add(new NodeComponentViewModel(nodeViewModel, component)
            {
                Name = component.ToString(),
            });
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

        var sourceNode = PendingConnection.Source;
        var targetNode = PendingConnection.Target;

        var connection = Connections.FirstOrDefault(e => e.Transitions.Any(e => e.Source == sourceNode && e.Target == targetNode));

        bool isSameLayer = sourceNode.LayerIndex == targetNode.LayerIndex;

        bool canConnect = connection is null && !sourceNode.IsEndNode && isSameLayer;
        bool canOverrideTransition = canConnect && sourceNode.IsLayerRootNode && isSameLayer; // Override transitions are only allowed in layer starts
        bool canLayerConnection = targetNode.IsLayerRootNode && !isSameLayer && !Connections.Any(e => e.Target == targetNode); // connections to root only, make sure nothing else connects

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
                Command = new RelayCommand<FsmNodeConnectionType>(ConnectNodes),
                Parameter = FsmNodeConnectionType.Normal,
            },
            new MenuItemViewModel()
            {
                Header = "Override Transition (only in layer starts)",
                Enabled = canOverrideTransition,
                IconKind = "Material.RayStartArrow",
                IconBrush = GraphColors.OverrideTransition,
                Command = new RelayCommand<FsmNodeConnectionType>(ConnectNodes),
                Parameter = FsmNodeConnectionType.Override,
            },
            MenuItemViewModel.Separator,
            new MenuItemViewModel()
            {
                Header = "New Layer Connection",
                Enabled = canLayerConnection,
                IconKind = "Material.Layers",
                Command = new RelayCommand<FsmNodeConnectionType>(ConnectNodes),
                Parameter = FsmNodeConnectionType.Layer,
            },
        ];

        var flyout = new MenuFlyout();
        flyout.ItemsSource = items;
        flyout.ShowAt(message.Response, showAtPointer: true);
    }

    /// <summary>
    /// Fired when dragging is completed, but previewed - before location assignments are done.<br/>
    /// Used to perform validation.
    /// </summary>
    /// <param name="obj"></param>
    [RelayCommand]
    private void OnPreviewItemsDragCompleted(DragCompletedEventArgs obj)
    {
        foreach (var item in SelectedNodes)
        {
            // Is the root node of a layer that's not the root?
            if (item is NodeViewModel nodeViewModel && nodeViewModel.IsLayerRootNode)
            {
                // Is the group also being moved?
                if (SelectedNodes.Contains(nodeViewModel.ParentGroup))
                    continue;

                // Preview new bbox
                Rect newNodeBbox = nodeViewModel.BoundaryBox.Translate(new Avalonia.Vector(obj.HorizontalChange, obj.VerticalChange));
                Rect newNodeCollision = newNodeBbox.Deflate(new Thickness(nodeViewModel.Size.Width * 0.25, nodeViewModel.Size.Height * 0.25));

                GroupNodeViewModel parentLayerGroup = nodeViewModel.ParentGroup;
                Rect groupBbox = parentLayerGroup.BoundaryBox;

                // Was it dragged outside of its layer?
                if (!groupBbox.Contains(newNodeCollision))
                {
                    obj.Canceled = true;
                    return;
                }
                else
                {
                    // Was it dragged in another layer?
                    foreach (GroupNodeViewModel group in LayerGroups.Values)
                    {
                        if (group == parentLayerGroup)
                            continue;

                        if (group.BoundaryBox.Contains(newNodeCollision))
                        {
                            obj.Canceled = true;
                            return;
                        }
                        
                    }
                }
            }
            else if (item is GroupNodeViewModel group)
            {
                Rect groupNewBbox = group.BoundaryBox.Translate(new Avalonia.Vector(obj.HorizontalChange, obj.VerticalChange));

                // Try not to drag whole group on other groups
                foreach (var otherGroup in LayerGroups.Values)
                {
                    if (otherGroup.LayerIndex == 0 || otherGroup == group)
                        continue;

                    if (groupNewBbox.Intersects(otherGroup.BoundaryBox))
                    {
                        obj.Canceled = true;
                        return;
                    }
                }

                // Check new nodes added?
                foreach (NodeViewModelBase nodeBase in Nodes)
                {
                    if (nodeBase is not NodeViewModel nodeVm)
                        continue;

                    if (groupNewBbox.Contains(nodeVm.CollisionBox))
                    {
                        if (nodeBase.LayerIndex == group.LayerIndex)
                            continue;

                        // The group has been dragged over a node that wasn't previously in this layer.

                        // We dragged the group over the root of another layer. No-go.
                        if (nodeVm.IsLayerRootNode)
                        {
                            obj.Canceled = true;
                            return;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Fired when node dragging is completed, used to perform layer assignments.
    /// </summary>
    /// <param name="data"></param>
    [RelayCommand]
    private void OnItemsDragCompleted(object data)
    {
        foreach (NodeViewModelBase node in SelectedNodes)
        {
            if (node is not NodeViewModel nodeVM)
                continue; 

            UpdateNodeLayerFromLocation(nodeVM);
        }

        foreach (NodeViewModelBase node in SelectedNodes)
        {
            if (node is not GroupNodeViewModel groupNodeVM)
                continue;

            UpdateNodesWithinLayerBoundary(groupNodeVM);
        }
    }

    /// <summary>
    /// Updates nodes on the graph based on the visual location of the specified layer group.
    /// </summary>
    /// <param name="layerGroup"></param>
    private void UpdateNodesWithinLayerBoundary(GroupNodeViewModel layerGroup)
    {
        Rect groupBbox = layerGroup.BoundaryBox;
        foreach (NodeViewModelBase nodeBase in Nodes)
        {
            if (nodeBase is not NodeViewModel nodeVm)
                continue;

            if (groupBbox.Contains(nodeVm.CollisionBox))
            {
                if (nodeVm.LayerIndex == layerGroup.LayerIndex)
                    continue;

                SetNodeToLayer(nodeVm, layerGroup);
            }
        }
    }

    [RelayCommand]
    private void OnLayerNodeRenamed(GroupNodeViewModel groupVm)
    {
        SolutionExplorerViewModel? solutionExplorerVM = App.Current?.Services?.GetRequiredService<SolutionExplorerViewModel>();
        if (solutionExplorerVM is null)
            return;

        TreeViewItemViewModel? tvi = solutionExplorerVM.GetItem(groupVm.Id);
        if (tvi is null)
        {
            // TODO: warn
            return;
        }

        tvi.TreeViewName = groupVm.Title;
    }

    /// <summary>
    /// Updates a node's layer relationship based on its visual location.
    /// </summary>
    /// <param name="node"></param>
    private void UpdateNodeLayerFromLocation(NodeViewModel node)
    {
        // Allow 1/4 on each edge
        var nodeCollision = node.CollisionBox;

        int i = 0;
        foreach (GroupNodeViewModel layer in LayerGroups.Values)
        {
            // Node is in layer/group boundary?
            var layerBbox = layer.BoundaryBox;
            if (nodeCollision.X >= layerBbox.X && nodeCollision.Right <= layerBbox.Right &&
                nodeCollision.Y >= layerBbox.Y && nodeCollision.Bottom <= layerBbox.Bottom)
            {
                // Is it a different layer?
                if (node.LayerIndex != layer.LayerIndex && layer.LayerIndex != 0)
                {
                    // Yes, update relationship
                    SetNodeToLayer(node, layer);
                }
                
                break;
            }

            // Are we at the end?
            if (i == LayerGroups.Count - 1)
            {
                // Was this node in a layer?
                if (node.LayerIndex != 0)
                {
                    // Yes, now it's not, so assign it to the root
                    var rootLayer = LayerGroups[0];
                    SetNodeToLayer(node, rootLayer);
                }
            }

            i++;
        }
    }

    private void SetNodeToLayer(NodeViewModel node, GroupNodeViewModel layerGroup)
    {
        node.LayerIndex = layerGroup.LayerIndex;
        node.ParentGroup.Nodes.Remove(node);

        layerGroup.Nodes.Add(node);
        node.ParentGroup = layerGroup;

        if (node.IsLayerRootNode && layerGroup.Nodes.Count != 0)
            node.SetRootNodeState(false);
        else if (layerGroup.Nodes.Count == 1)
            node.SetRootNodeState(true);
    }

    public enum FsmNodeConnectionType
    {
        Normal,
        Override,
        Layer
    }

    /// <summary>
    /// Applies the current pending connection with the specified connection type.
    /// </summary>
    /// <param name="connectType"></param>
    public void ConnectNodes(FsmNodeConnectionType connectType)
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

            if (connectType == FsmNodeConnectionType.Override)
                existingConnection.ArrowColor = GraphColors.OverrideTransition;
            else if (connectType == FsmNodeConnectionType.Normal)
                existingConnection.ArrowColor = GraphColors.NormalTransition;
            else if (connectType == FsmNodeConnectionType.Layer)
                existingConnection.SetAsLayerConnection();

            Connections.Add(existingConnection);
        }


        var transition = new TransitionViewModel(existingConnection)
        {
            Source = PendingConnection.Source,
            Target = PendingConnection.Target,
            IsOverrideTransition = connectType == FsmNodeConnectionType.Override,
        };
        PendingConnection.Source.Transitions.Add(transition);
        if (PendingConnection.Source == PendingConnection.Target)
            PendingConnection.Target.Transitions.Add(transition);

        existingConnection?.Transitions.Add(transition);
        if (connectType != FsmNodeConnectionType.Layer)
        {
            WeakReferenceMessenger.Default.Send(new EditConnectionRequest(existingConnection!));
        }
    }

    /// <summary>
    /// Creates a node view model for the specified fsm node.
    /// </summary>
    /// <param name="node">FSM file source node</param>
    /// <param name="nodeList">List of all nodes</param>
    /// <param name="depth">Current depth</param>
    /// <returns></returns>
    private NodeViewModel? CreateViewModelFromFSMNode(FSMNode node, List<FSMNode> nodeList, ref int depth, bool loadEditorParameters)
    {
        if (_processedNodes.Contains(node.Guid))
            return null;

        _processedNodes.Add(node.Guid);

        NodeViewModel graphNode = GetNodeViewModel(node, depth * 400, 0, loadEditorParameters);
        AddNode(graphNode);

        if (node.ChildLayerId != -1) // New layer
        {
            int depth_ = depth + 1;
            NodeViewModel? subLayerNode = CreateViewModelFromFSMNode(node.Children[0], nodeList, ref depth_, loadEditorParameters);
            if (subLayerNode is not null)
            {
                GraphConnectionViewModel connection = new()
                {
                    Source = graphNode,
                    Target = subLayerNode,
                };
                connection.SetAsLayerConnection();

                if (!loadEditorParameters)
                    graphNode.Title += $" (to Layer{node.ChildLayerId})";

                graphNode.Transitions.Add(new TransitionViewModel(connection)
                {
                    Source = graphNode,
                    Target = subLayerNode,
                });
                Connections.Add(connection);

                if (!LayerGroups.TryGetValue(node.ChildLayerId, out GroupNodeViewModel groupNode)!)
                {
                    groupNode = new GroupNodeViewModel()
                    {
                        ParentEditor = this,
                        LayerIndex = node.ChildLayerId,
                        Title = !string.IsNullOrEmpty(node.ChildLayerName) ? node.ChildLayerName : $"Layer {node.ChildLayerId}",
                    };

                    if (loadEditorParameters)
                    {
                        groupNode.Location = new Point(node.ChildLayerBoundaryBox.X, node.ChildLayerBoundaryBox.Y);
                        groupNode.Size = new Size(node.ChildLayerBoundaryBox.Z, node.ChildLayerBoundaryBox.W);
                    }

                    if (groupNode.LayerIndex != 0)
                        Nodes.Add(groupNode);

                    LayerGroups.Add(node.ChildLayerId, groupNode);
                    subLayerNode.SetRootNodeState(true);
                }
            }
        }

        for (int i = 0; i < node.RegularTransitions.Count; i++)
        {
            Transition trans = node.RegularTransitions[i];
            AddTransition(trans, node, nodeList, depth, graphNode, i, loadEditorParameters: loadEditorParameters);
        }

        for (int i = 0; i < node.OverrideTransitions.Count; i++)
        {
            Transition trans = node.OverrideTransitions[i];
            AddTransition(trans, node, nodeList, depth, graphNode, i, true, loadEditorParameters: loadEditorParameters);
        }

        return graphNode;
    }

    private void AddNode(NodeViewModel node)
    {
        RegisterFsmElementGuid(node.Guid, node);
        Nodes.Add(node);

        foreach (NodeComponentViewModel componentVM in node.Components)
        {
            RegisterFsmElementGuid(componentVM.Component.Guid, componentVM.Component);
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
        } while (_guidToFsmElement.ContainsKey(guid));

        return guid;
    }

    /// <summary>
    /// Registers a graph component (node, bt component) to the guid mapping.
    /// </summary>
    /// <param name="guid"></param>
    /// <param name="elem"></param>
    public void RegisterFsmElementGuid(uint guid, object elem)
    {
        _guidToFsmElement.TryAdd(guid, elem);
    }

    /// <summary>
    /// Unregisters a graph component from the guid mapping.
    /// </summary>
    /// <param name="guid"></param>
    public void UnregisterFsmElementGuid(uint guid)
    {
        _guidToFsmElement.Remove(guid);
    }

    /// <summary>
    /// Creates a transition.
    /// </summary>
    /// <param name="trans">FSM file source transition.</param>
    /// <param name="sourceNode">FSM file source node.</param>
    /// <param name="allNodesList">List of all nodes.</param>
    /// <param name="depth">Current depth.</param>
    /// <param name="sourceNodeVm">Source node view model.</param>
    /// <param name="i">Index (for location placement, before auto-layouting).</param>
    /// <param name="isOverrideTransition">Whether this is an override transition.</param>
    /// <param name="loadEditorParameters">Whether to load editor parameters such as node names and node locations.</param>
    private void AddTransition(Transition trans, FSMNode sourceNode, List<FSMNode> allNodesList, int depth, NodeViewModel sourceNodeVm, int i, 
        bool isOverrideTransition = false, bool loadEditorParameters = false)
    {
        FSMNode? toFsmNode = sourceNode.Children.FirstOrDefault(e => e.Guid == trans.FromNodeGuid);

        // This is kinda weird, we're gonna use the full node list here, but this shouldn't ever be needed - at best the parent node is used
        toFsmNode ??= allNodesList.FirstOrDefault(e => e.Guid == trans.FromNodeGuid);

        NodeViewModel toNode;
        if (toFsmNode is null)
        {
            if (trans.IsEndTransition)
            {
                toFsmNode = new FSMNode(trans.FromNodeGuid)
                {
                    LayerIndex = sourceNode.LayerIndex,
                };

                if (loadEditorParameters)
                {
                    toFsmNode.Name = trans.EndNodeName;
                    toFsmNode.BoundaryBox = trans.EndNodeBoundaryBox;
                }
            }
            else
            {
                // This branch is invalid?
                Debug.WriteLine($"WARN: A transition refers to node {trans.FromNodeGuid}, but it does not exist in node list");
                return;
            }
        }

        // regular node.
        toNode = GetNodeViewModel(toFsmNode, (depth + 1) * 400, i * 200, loadEditorParameters: loadEditorParameters);
        if (trans.IsEndTransition)
        {
            toNode.Title = "END";
            toNode.IsEndNode = true;
            toNode.UpdateBorderColor();
        }

        if (!_processedNodes.Contains(toFsmNode.Guid))
        {
            int depth_ = depth + 1;
            CreateViewModelFromFSMNode(toFsmNode, allNodesList, ref depth_, loadEditorParameters: loadEditorParameters);
        }

        GraphConnectionViewModel? connection;
        if (trans.ToNodeGuid == trans.FromNodeGuid)
            connection = Connections.FirstOrDefault(e => (e.Source == sourceNodeVm || e.Source == toNode) &&
                                                         (e.Target == sourceNodeVm || e.Target == toNode));
        else
            connection = Connections.FirstOrDefault(e => (e.Source == sourceNodeVm || e.Source == toNode) &&
                                                          (e.Target == sourceNodeVm || e.Target == toNode) && e.Source != e.Target);

        if (connection is null)
        {
            connection = new GraphConnectionViewModel
            {
                Source = sourceNodeVm,
                Target = toNode,
                ArrowColor = isOverrideTransition ? GraphColors.OverrideTransition : GraphColors.NormalTransition,
            };
            Connections.Add(connection);

            if (connection.Source == connection.Target)
            {
                sourceNodeVm.HasSelfTransition = true;
            }
        }
        else
        {
            connection.ArrowHeadEnds = ArrowHeadEnds.Both;
        }

        TransitionViewModel transition = new(connection)
        {
            Source = sourceNodeVm,
            Target = toNode,
            IsOverrideTransition = isOverrideTransition,
        };
        transition.Source.Transitions.Add(transition);

        connection.Transitions.Add(transition);

        if (trans.ConditionComponents.Count != 0)
        {
            for (int j = 0; j < trans.ConditionComponents.Count; j++)
            {
                if (j != 0)
                {
                    int paramsIndex = j - 1;
                    Transition.TransitionParam param = trans.TransitionParams[paramsIndex];

                    var opVm = new TransitionConditionOpViewModel() // Do not simplify. OnOperandChanged must be called for Operand
                    {
                        Priority = param.Priority,
                    };
                    opVm.Operand = param.IsAndCondition ? TransitionOperandType.AND : TransitionOperandType.OR;
                    transition.ConditionComponents.Add(opVm);
                }

                transition.ConditionComponents.Add(new TransitionConditionViewModel(trans.ConditionComponents[j])
                {
                    Title = trans.ConditionComponents[j].ComponentName,
                    IsFalse = trans.ConditionComponents[j].IsReverseSuccess,
                });
            }

            connection.UpdateConnection();
        }
    }

    private readonly Dictionary<uint, NodeViewModel> _guidToNodeVm = [];
    private readonly HashSet<uint> _processedNodes = [];

    private FSMState BuildTreeFromCurrentGraph()
    {
        var state = new FSMState() 
        { 
            EditorSettings =
            {
                ViewportLocation = new Vector2((float)this.ViewportLocation.X, (float)this.ViewportLocation.Y),
                ViewportSize = new Vector2((float)this.ViewportSize.Width, (float)this.ViewportSize.Height),
                ViewportZoom = (float)this.ViewportZoom,
            }
        };

        Dictionary<uint, FSMNode> processedNodes = [];

        var rootNode = CreateFSMNodeFromViewModel(_rootNode);
        processedNodes.TryAdd(rootNode.Guid, rootNode);
        state.Layers.Add(0, [rootNode]);

        BuildFSMNode(state, _rootNode, rootNode, processedNodes);
        return state;
    }

    private void BuildFSMNode(FSMState fsmState, NodeViewModel nodeVM, FSMNode fsmNode, Dictionary<uint, FSMNode> processedNodes)
    {
        fsmNode.FsmFolderName = nodeVM.FsmFolderName;
        fsmNode.FsmName = nodeVM.FsmName;
        fsmNode.NameHash = nodeVM.NameHash;

        foreach (NodeComponentViewModel componentViewModel in nodeVM.Components)
            fsmNode.ExecutionComponents.Add(componentViewModel.Component);

        foreach (var transition in nodeVM.Transitions)
        {
            NodeViewModel sourceNvm = (NodeViewModel)_guidToFsmElement[transition.Source.Guid];
            NodeViewModel targetNvm = (NodeViewModel)_guidToFsmElement[transition.Target.Guid];

            // Layer to layer transitions don't emit transition nodes, so only do same layer.
            if (transition.Source.LayerIndex == transition.Target.LayerIndex)
            {
                Transition trans = BuildFSMTransition(processedNodes, transition, fsmNode);
                if (transition.IsOverrideTransition)
                {
                    trans.ToNodeGuid = 0;
                    fsmNode.OverrideTransitions.Add(trans);
                }
                else
                {
                    fsmNode.RegularTransitions.Add(trans);
                }

                if (targetNvm.IsEndNode)
                {
                    trans.IsEndTransition = true;

                    var nodeBbox = transition.Target.BoundaryBox;
                    trans.EndNodeName = transition.Target.FsmName;
                    trans.EndNodeBoundaryBox = new Vector4((float)nodeBbox.X, (float)nodeBbox.Y,
                        (float)nodeBbox.Width, (float)nodeBbox.Height);
                }
            }

            // Process source
            if (!processedNodes.TryGetValue(transition.Source.Guid, out FSMNode? sourceFsmNode))
            {
                sourceFsmNode = CreateFSMNodeFromViewModel(transition.Source);
                processedNodes.Add(sourceFsmNode.Guid, sourceFsmNode);

                BuildFSMNode(fsmState, sourceNvm, sourceFsmNode, processedNodes);
            }

            // Process target, and layer transitions if needed.
            if (!processedNodes.TryGetValue(transition.Target.Guid, out FSMNode? targetFsmNode))
            {
                targetFsmNode = CreateFSMNodeFromViewModel(transition.Target);
                processedNodes.Add(targetFsmNode.Guid, targetFsmNode);

                // Create or fetch new layer.
                if (!fsmState.Layers.TryGetValue(transition.Target.LayerIndex, out List<FSMNode>? nodesForLayer))
                {
                    nodesForLayer = [];
                    fsmState.Layers.Add(transition.Target.LayerIndex, nodesForLayer);
                }

                if (transition.Source.LayerIndex != transition.Target.LayerIndex)
                {
                    GroupNodeViewModel layerGroupVm = this.LayerGroups[transition.Target.LayerIndex];
                    sourceFsmNode.ChildLayerId = transition.Target.LayerIndex;
                    sourceFsmNode.ChildLayerName = layerGroupVm.Title;

                    var rect = layerGroupVm.BoundaryBox;
                    sourceFsmNode.ChildLayerBoundaryBox = new Vector4((float)rect.X, (float)rect.Y, (float)rect.Width, (float)rect.Height);

                    // Counts as one child.
                    sourceFsmNode.Children.Add(targetFsmNode);
                }

                // Reminder, end nodes aren't actually emitted as a node on the fsm file.
                if (!targetNvm.IsEndNode)
                    fsmState.Layers[transition.Target.LayerIndex].Add(targetFsmNode);

                // Register children - which is also used for setting TailIndexOfChildNodeGuids.
                // Children is only present on layer root nodes, and also nodes that transition to other layers (usually just 1 node).
                FSMNode layerRoot = nodesForLayer[0];
                if (!layerRoot.Children.Contains(targetFsmNode) &&
                    !targetNvm.IsEndNode  // End nodes don't count as physical nodes so they aren't children.
                    && transition.Source.LayerIndex == transition.Target.LayerIndex)
                {
                    layerRoot.Children.Add(targetFsmNode);
                }

                BuildFSMNode(fsmState, targetNvm, targetFsmNode, processedNodes);
            }
        }

        if (fsmNode.ChildLayerId == -1)
            fsmNode.TailIndexOfChildNodeGuids = fsmNode.Children.Count;
        else
            fsmNode.TailIndexOfChildNodeGuids = 0;
    }

    private static FSMNode CreateFSMNodeFromViewModel(NodeViewModel nodeVM)
    {
        return new FSMNode(nodeVM.Guid)
        {
            NameHash = CRC32.crc32_0x77073096(nodeVM.Title ?? "FSMNode"),
            Name = nodeVM.Title,
            BoundaryBox = new Vector4((float)nodeVM.Location.X, (float)nodeVM.Location.Y, (float)nodeVM.Size.Width, (float)nodeVM.Size.Height),
        };
    }

    private static Transition BuildFSMTransition(Dictionary<uint, FSMNode> processedNodes, TransitionViewModel transition, FSMNode node)
    {
        Transition fsmTransition = new(transition.Source.Guid, transition.Target.Guid);
        foreach (TransitionConditionBase conditionBase in transition.ConditionComponents)
        {
            if (conditionBase is TransitionConditionViewModel conditionComponent)
            {
                fsmTransition.ConditionComponents.Add(conditionComponent.ConditionComponent);
                fsmTransition.ConditionGuids.Add(conditionComponent.ConditionComponent.Guid);
            }
            else if (conditionBase is TransitionConditionOpViewModel operand)
            {
                fsmTransition.TransitionParams.Add(new Transition.TransitionParam()
                {
                    Priority = operand.Priority,
                    IsAndCondition = operand.Operand == TransitionOperandType.AND,
                });
            }
        }

        return fsmTransition;
    }
}
