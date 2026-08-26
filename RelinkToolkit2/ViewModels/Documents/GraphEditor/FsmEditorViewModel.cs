using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform.Storage;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using Dock.Controls.DeferredContentControl;

using GBFRDataTools.FSM;
using GBFRDataTools.FSM.Components;
using GBFRDataTools.FSM.Components.Conditions;
using GBFRDataTools.FSM.Entities;
using GBFRDataTools.Hashing;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using MsBox.Avalonia;

using Nodify;
using Nodify.Compatibility;

using RelinkToolkit2.Messages.Fsm;
using RelinkToolkit2.Services;
using RelinkToolkit2.ViewModels.Documents.GraphEditor.Nodes;
using RelinkToolkit2.ViewModels.Documents.GraphEditor.TransitionComponents;
using RelinkToolkit2.ViewModels.Documents.Interfaces;
using RelinkToolkit2.ViewModels.Menu;
using RelinkToolkit2.ViewModels.TreeView;

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace RelinkToolkit2.ViewModels.Documents.GraphEditor;

public partial class FsmEditorViewModel : NodeEditorViewModelBase, ISaveableDocument, IGraphEditor,
    IRecipient<TransitionComponentAddRequest>,
    IRecipient<NodeGraphSelectionChangeRequest>,
    IRecipient<DeleteNodeConnectionRequest>
{
    private readonly ILogger? _logger;
    private readonly DialogService _dialogService;

    /// <summary>
    /// All displayed nodes in the graph (including groups, except layer 0).
    /// </summary>
    public ObservableCollection<FsmNodeViewModelBase> Nodes { get; } = [];

    /// <summary>
    /// Connections in the graph.
    /// </summary>
    public ObservableCollection<FsmConnectionViewModel> Connections { get; } = [];

    /// <summary>
    /// All layers (as a group) on the graph.<br/>
    /// The root layer (layer 0) is included.
    /// </summary>
    public Dictionary<int, FsmGroupNodeViewModel> LayerGroups { get; set; } = [];

    [ObservableProperty]
    private PendingConnectionViewModel _pendingConnection = new();

    /// <summary>
    /// Currently selected node.
    /// </summary>
    [ObservableProperty]
    private FsmNodeViewModel? _selectedNode;

    /// <summary>
    /// Currently selected connection.
    /// </summary>
    [ObservableProperty]
    private FsmConnectionViewModel? _selectedConnection;

    /// <summary>
    /// Currently selected nodes.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<FsmNodeViewModelBase> _selectedNodes = [];

    private string? _currentFsmName;

    private FsmNodeViewModel? _rootNode;

    public bool ShouldPromptAsFirstSave { get; set; } = true;

    /// <summary>
    /// Menu items for the main context menu (add nodes, etc).
    /// </summary>
    public ObservableCollection<MenuItemViewModel> EditorContextMenuItems { get; set; } = [];
    public MenuItemViewModel AddLayerMenuItem { get; set; }


    /// <summary>
    /// Quest context. Used for Quest FSMs.
    /// </summary>
    private QuestContext? _questContext;

    public FsmEditorViewModel()
    {
        _logger = App.Current.Services.GetService<ILogger<FsmEditorViewModel>>();
        _dialogService = App.Current.Services.GetService<DialogService>();

        // We are essentially swapping out gestures here.
        // We want group layer click to select the whole layer. not just the group.
        //EditorGestures.Mappings.ItemContainer.Selection.Replace.Value = new MouseGesture(MouseAction.LeftClick, KeyModifiers.Control);
        //EditorGestures.Mappings.ItemContainer.Selection.Invert.Value = new MouseGesture(MouseAction.LeftClick);

        var rootLayer = new FsmGroupNodeViewModel() { ParentEditor = this, LayerIndex = 0 };
        LayerGroups.Add(0, rootLayer);

        if (Design.IsDesignMode)
        {
            var node = new FsmNodeViewModel()
            {
                ParentEditor = this,
                Title = "Test1",
                Guid = 123456789,
                FsmSource = "playerai/test",
                IsLayerRootNode = true,
                Location = new Point(-200, 0),
                ParentGroup = rootLayer,
            };
            node.UpdateVisuals();
            AddNode(node);

            var node2 = new FsmNodeViewModel()
            {
                ParentEditor = this,
                Title = "Test2",
                Guid = 12345678,
                FsmSource = "playerai/test",
                IsEndNode = true,
                IsBranch = true,
                Location = new Point(-100, 0),
                ParentGroup = rootLayer,
            };
            node2.UpdateVisuals();
            AddNode(node2);

            Connections.Add(new FsmConnectionViewModel()
            {
                Source = node,
                Target = node2,
            });

            var selfTransition = new FsmConnectionViewModel()
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

            var layerGroup = new FsmGroupNodeViewModel()
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
        WeakReferenceMessenger.Default.RegisterAll(this);
    }

    public override void UnregisterMessageListeners()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    /// <summary>
    /// Fired when selecting a node manually
    /// </summary>
    /// <param name="message"></param>
    public void Receive(NodeGraphSelectionChangeRequest message)
    {
        SelectedNode = (FsmNodeViewModel)message.Node;
        message.Reply(true);
    }

    /// <summary>
    /// Fired when deleting a node
    /// </summary>
    /// <param name="message"></param>
    public void Receive(DeleteNodeConnectionRequest message)
    {
        GraphConnectionViewModel connection = message.Connection;

        bool result = Connections.Remove((FsmConnectionViewModel)connection);
        message.Reply(result);
    }

    /// <summary>
    /// Fired when a component selection was complete
    /// </summary>
    /// <param name="message"></param>
    public void Receive(TransitionComponentAddRequest message)
    {
        ConditionComponent component = message.Component;
        TransitionConditionOpViewModel operatorGroup = message.OperatorGroup;

        component.Guid = GetNewGuid();
        RegisterFsmElementGuid(component.Guid, component);

        operatorGroup.AddElement(new TransitionConditionViewModel(component, operatorGroup)
        {
            Title = component.ComponentName,
            IsExpanded = true
        });
        operatorGroup.ParentConnection.UpdateConnection();
        message.Reply(true);
    }

    /// <summary>
    /// Resets graph state.
    /// </summary>
    private void ResetGraph()
    {
        _processedNodes.Clear();
        _guidToElement.Clear();

        Nodes.Clear();
        Connections.Clear();

        SelectedNode = null;
        SelectedNodes.Clear();

        LayerGroups.Clear();

        var rootLayer = new FsmGroupNodeViewModel() { ParentEditor = this, LayerIndex = 0 };
        LayerGroups.Add(0, rootLayer);
    }

    /// <summary>
    /// Called by generated observable property <see cref="_selectedNode"/>
    /// </summary>
    /// <param name="value"></param>
    partial void OnSelectedNodeChanged(FsmNodeViewModel? value)
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
    partial void OnSelectedConnectionChanged(FsmConnectionViewModel? value)
    {
        var srcFsmNode = value?.Source as FsmNodeViewModel;
        var dstFsmNode = value?.Source as FsmNodeViewModel;
        if (srcFsmNode?.LayerIndex != dstFsmNode?.LayerIndex)
            return;

        WeakReferenceMessenger.Default.Send(new EditConnectionRequest(value));
    }

    /// <summary>
    /// Saves the graph as a FSM file.
    /// </summary>
    /// <param name="fileName"></param>
    public async Task<Uri?> SaveDocument(IFilesService filesService, bool isSaveAs = false)
    {
        if (_rootNode is null)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", $"There is no root node in this FSM.", icon: MsBox.Avalonia.Enums.Icon.Error);
            await _dialogService.ShowMessageBoxDialogAsync(box);
            return null;
        }

        Uri? outputUri = LastFile;
        if (isSaveAs || outputUri is null || ShouldPromptAsFirstSave)
        {
            string defaultSaveFileName;
            if (LastFile is not null)
                defaultSaveFileName = Path.GetFileName(LastFile.LocalPath);
            else
                defaultSaveFileName = $"{Title}_fsm_ingame.json";

            var file = await filesService.SaveFileAsync("Save FSM file", null, defaultSaveFileName, LastFile);
            if (file is null)
                return null;

            LastFile = file.Path;
        }

        string? outputPath = LastFile?.LocalPath;
        if (string.IsNullOrWhiteSpace(outputPath))
            return null;

        FSMState buildState = BuildTreeFromCurrentGraph();
        using Stream stream = File.Create(outputPath);
        var builder = new FSMSerializer(buildState);
        builder.WriteJson(stream);

        LastFile = outputUri;

        string newTitle = Path.GetFileNameWithoutExtension(outputPath);
        if (newTitle.EndsWith("_fsm_ingame"))
            newTitle = newTitle.Replace("_fsm_ingame", string.Empty);

        _currentFsmName = newTitle;
        Title = _currentFsmName;
        SolutionTreeViewItem.TreeViewName = _currentFsmName;

        if (buildState.HasOrphanNodes)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Warning", $"File has been saved but has orphan/disconnected nodes, check log window.\n", icon: MsBox.Avalonia.Enums.Icon.Warning);
            await _dialogService.ShowMessageBoxDialogAsync(box);
        }

        int emptyLayers = buildState.Layers.Count(e => e.Value.Count == 0);
        if (emptyLayers > 0)
        {
            IEnumerable<int> emptyLayerIndices = buildState.Layers.Where(e => e.Value.Count == 0).Select(e => e.Key);
            var box = MessageBoxManager.GetMessageBoxStandard("Warning", $"File has been saved but has empty layers (layer {string.Join(", ", emptyLayerIndices)})\n" +
                $"Disconnected layers will not be saved!", icon: MsBox.Avalonia.Enums.Icon.Warning);
            await _dialogService.ShowMessageBoxDialogAsync(box);
        }

        ShouldPromptAsFirstSave = false;
        return outputUri;
    }

    /// <summary>
    /// Inits the graph from the specified parser.
    /// </summary>
    public async Task<bool> InitGraph(string fsmName, FSMParser? fsmParser = null)
    {
        _currentFsmName = fsmName;
        ResetGraph();

        if (fsmParser is not null)
        {
            try
            {
                if (fsmParser.RootNode is null)
                {
                    foreach (var group in fsmParser.LayersToNodes)
                    {
                        if (group.Count == 0)
                            continue;

                        int depth = 0;
                        TryGetOrCreateGraphNode(group[0], fsmParser.LayersToNodes, ref depth, fsmParser.EditorSettings is not null, out _);
                    }
                }
                else
                {
                    int depth = 0;
                    bool rootWasCreated = TryGetOrCreateGraphNode(fsmParser.RootNode, fsmParser.LayersToNodes, ref depth, fsmParser.EditorSettings is not null, out FsmNodeViewModel rootNode);

                    // Reference nodes for each layer
                    foreach (FsmNodeViewModelBase node in Nodes)
                    {
                        if (node is FsmNodeViewModel nodeViewModel)
                        {
                            var group = LayerGroups[node.LayerIndex];
                            group.Nodes.Add(nodeViewModel);
                            nodeViewModel.ParentGroup = group;
                        }
                    }

                    if (rootWasCreated)
                    {
                        rootNode.SetRootNodeState(true);
                        _rootNode = rootNode;
                    }
                }
            }
            catch (Exception ex)
            {
                var box = MessageBoxManager.GetMessageBoxStandard("Error", $"Failed to setup the graph for the FSM.\n{ex.Message}", icon: MsBox.Avalonia.Enums.Icon.Error);
                await _dialogService.ShowMessageBoxDialogAsync(box);
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
        }

        return true;
    }

    /// <summary>
    /// Adds a new node to the graph, using the current mouse location.
    /// </summary>
    public void AddNewNode()
    {
        var node = new FsmNodeViewModel
        {
            ParentEditor = this,
            Guid = GetNewGuid(),
            Location = MouseLocation,
            IsRenaming = true,
            Title = "FSMNode",
            ParentGroup = LayerGroups[0]
        };

        AddNode(node);
        UpdateNodeLayerFromLocation(node);
    }

    /// <summary>
    /// Adds a new node to the graph, using the current mouse location on the graph.
    /// </summary>
    public void AddNewLayer()
    {
        int layerIndex = LayerGroups.MaxBy(e => e.Key).Key + 1;

        var layerGroup = new FsmGroupNodeViewModel()
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
    public void RemoveNode(FsmNodeViewModel nodeVm)
    {
        for (int i = Connections.Count - 1; i >= 0; i--)
        {
            FsmConnectionViewModel? connection = Connections[i];
            if (connection.Target == nodeVm || connection.Source == nodeVm)
            {
                Connections.Remove(connection);
                RemoveAllTransitionsOfNodeToTargetNode((FsmNodeViewModel)connection.Source, nodeVm.Guid);
                RemoveAllTransitionsOfNodeToTargetNode((FsmNodeViewModel)connection.Target, nodeVm.Guid);
            }
        }

        Nodes.Remove(nodeVm);
        UnregisterFsmElementGuid(nodeVm.Guid);

        if (_rootNode == nodeVm)
            _rootNode = null;
    }

    /// <summary>
    /// Removes a layer from the graph.
    /// </summary>
    /// <param name="layerGroup"></param>
    public void RemoveLayer(FsmGroupNodeViewModel layerGroup)
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
    public void RemoveConnection(FsmConnectionViewModel connection)
    {
        RemoveAllTransitionsOfNodeToTargetNode((FsmNodeViewModel)connection.Source, connection.Source.Guid);
        RemoveAllTransitionsOfNodeToTargetNode((FsmNodeViewModel)connection.Target, connection.Target.Guid);

        Connections.Remove(connection);
    }

    /// <summary>
    /// Removes all transitions to the specified node.
    /// </summary>
    /// <param name="nodeVM"></param>
    /// <param name="guid"></param>
    private void RemoveAllTransitionsOfNodeToTargetNode(FsmNodeViewModel nodeVM, uint guid)
    {
        for (int i = nodeVM.Transitions.Count - 1; i >= 0; i--)
        {
            TransitionViewModel transition = nodeVM.Transitions[i];
            if (transition.Source.Guid == guid || transition.Target.Guid == guid)
            {
                foreach (TransitionConditionOpViewModel component in transition.ConditionOperatorGroups)
                    UnregisterGuidRecursive(component);

                nodeVM.Transitions.Remove(transition);
            }
        }

        void UnregisterGuidRecursive(TransitionConditionOpViewModel operatorGroup)
        {
            foreach (TransitionConditionElementViewModelBase component in operatorGroup.Elements)
            {
                if (component is TransitionConditionViewModel condition)
                    UnregisterFsmElementGuid(condition.ConditionComponent.Guid);
                else if (component is TransitionConditionOpViewModel subOperatorGroup)
                    UnregisterGuidRecursive(subOperatorGroup);
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
    private FsmNodeViewModel CreateNodeViewModel(FSMNode node, int x, int y, bool loadEditorParameters = false)
    {
        if (_guidToNodeVm.TryGetValue(node.Guid, out FsmNodeViewModel? nodeViewModel))
            return nodeViewModel;

        string? title;
        if (loadEditorParameters && !string.IsNullOrWhiteSpace(node.Name))
        {
            title = node.Name;
        }
        else
        {
            title = NodeNameStore.TryGetNameForNode(_currentFsmName, node.Guid, node.NameHash);
            if (string.IsNullOrWhiteSpace(title))
                title = $"{node.Guid}";
        }

        Point location;
        if (loadEditorParameters)
            location = new Point(node.BoundaryBox.X, node.BoundaryBox.Y);
        else
            location = new Point(x, y);

        nodeViewModel = new FsmNodeViewModel()
        {
            ParentEditor = this,
            Guid = node.Guid,
            Title = title,
            Location = location,
            LayerIndex = node.LayerIndex,
            NameHash = node.NameHash,
            FsmFolderName = node.FsmFolderName,
            FsmName = node.FsmName,
            IsBranch = node.IsBranch,
        };

        if (loadEditorParameters)
            nodeViewModel.Size = new Size(node.BoundaryBox.Z, node.BoundaryBox.W);

        if (!string.IsNullOrWhiteSpace(node.FsmFolderName) && !string.IsNullOrWhiteSpace(node.FsmName))
            nodeViewModel.SetBaseFsm(node.FsmFolderName, node.FsmName);

        foreach (BehaviorTreeComponent component in node.ExecutionComponents)
        {
            nodeViewModel.Components.Add(new NodeComponentViewModel(nodeViewModel, component)
            {
                Name = component.ToString(),
            });
        }

        nodeViewModel.UpdateVisuals();
        _guidToNodeVm.Add(node.Guid, nodeViewModel);

        return nodeViewModel;
    }

    [RelayCommand]
    private void OnConnectionCompleted(FsmNodeViewModel? target)
    {
        if (target is null)
            return;

        if (PendingConnection.Source == PendingConnection.Target)
            return;

        var sourceNode = (FsmNodeViewModel)PendingConnection.Source;
        var targetNode = (FsmNodeViewModel)PendingConnection.Target;

        var connection = Connections.FirstOrDefault(e => e.Transitions.Any(e => e.Source == sourceNode && e.Target == targetNode));

        bool isSameLayer = sourceNode.LayerIndex == targetNode.LayerIndex;

        bool canConnect = connection is null && !sourceNode.IsEndNode && isSameLayer;
        bool canOverrideTransition = canConnect && sourceNode.IsLayerRootNode && isSameLayer; // Override transitions are only allowed in layer starts

        // for layer connections - connections to root only, make sure nothing else connects
        // Previously it was assumed that a layer can only be connected from one node - but this is not true (issue #1)
        bool canLayerConnection = targetNode.IsLayerRootNode && !isSameLayer
            // && !Connections.Any(e => e.Target == targetNode) 
            && !sourceNode.Transitions.Any(e => e.Source.LayerIndex != e.Target.LayerIndex);

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

        WeakReferenceMessenger.Default.Send(new OpenContextMenuFlyoutAtNode(target, items));
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
            if (item is FsmNodeViewModel nodeViewModel && nodeViewModel.IsLayerRootNode)
            {
                // Is the group also being moved?
                if (SelectedNodes.Contains(nodeViewModel.ParentGroup))
                    continue;

                // Preview new bbox
                Rect newNodeBbox = nodeViewModel.BoundaryBox.Translate(new Avalonia.Vector(obj.HorizontalChange, obj.VerticalChange));
                Rect newNodeCollision = newNodeBbox.Deflate(new Thickness(nodeViewModel.Size.Width * 0.25, nodeViewModel.Size.Height * 0.25));

                FsmGroupNodeViewModel parentLayerGroup = nodeViewModel.ParentGroup;
                Rect groupBbox = parentLayerGroup.BoundaryBox;

                // Was it dragged outside of its layer?
                if (!groupBbox.Contains(newNodeCollision) && !nodeViewModel.IsLayerRootNode)
                {
                    obj.Canceled = true;
                    return;
                }
                else
                {
                    // Was it dragged in another layer?
                    foreach (FsmGroupNodeViewModel group in LayerGroups.Values)
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
            else if (item is FsmGroupNodeViewModel group)
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
                foreach (FsmNodeViewModelBase nodeBase in Nodes)
                {
                    if (nodeBase is not FsmNodeViewModel nodeVm)
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
        foreach (FsmNodeViewModelBase node in SelectedNodes)
        {
            if (node is not FsmNodeViewModel nodeVM)
                continue;

            UpdateNodeLayerFromLocation(nodeVM);
        }

        foreach (NodeViewModelBase node in SelectedNodes)
        {
            if (node is not FsmGroupNodeViewModel groupNodeVM)
                continue;

            UpdateNodesWithinLayerBoundary(groupNodeVM);
        }
    }

    /// <summary>
    /// Updates nodes on the graph based on the visual location of the specified layer group.
    /// </summary>
    /// <param name="layerGroup"></param>
    private void UpdateNodesWithinLayerBoundary(FsmGroupNodeViewModel layerGroup)
    {
        Rect groupBbox = layerGroup.BoundaryBox;
        foreach (NodeViewModelBase nodeBase in Nodes)
        {
            if (nodeBase is not FsmNodeViewModel nodeVm)
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
    private void OnLayerNodeRenamed(FsmGroupNodeViewModel groupVm)
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
    private void UpdateNodeLayerFromLocation(FsmNodeViewModel node)
    {
        // Allow 1/4 on each edge
        var nodeCollision = node.CollisionBox;

        int i = 0;
        foreach (FsmGroupNodeViewModel layer in LayerGroups.Values)
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

    private void SetNodeToLayer(FsmNodeViewModel node, FsmGroupNodeViewModel layerGroup)
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

        FsmConnectionViewModel? existingConnection = Connections.FirstOrDefault(e => e.Source == PendingConnection.Source && e.Target == PendingConnection.Target ||
                                                                                  e.Target == PendingConnection.Source && e.Source == PendingConnection.Target);

        if (existingConnection is null)
        {
            existingConnection = new FsmConnectionViewModel()
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

        FsmNodeViewModel source = (FsmNodeViewModel)PendingConnection.Source;
        FsmNodeViewModel target = (FsmNodeViewModel)PendingConnection.Target;
        var transition = new TransitionViewModel(existingConnection)
        {
            Source = source,
            Target = target,
            IsOverrideTransition = connectType == FsmNodeConnectionType.Override,
        };
        source.Transitions.Add(transition);
        if (source == target)
            target.Transitions.Add(transition);

        existingConnection?.Transitions.Add(transition);
        if (connectType != FsmNodeConnectionType.Layer)
        {
            WeakReferenceMessenger.Default.Send(new EditConnectionRequest(existingConnection!));
        }
    }

    /// <summary>
    /// Gets or create a new graph node on the graph.
    /// </summary>
    /// <param name="node">FSM file source node</param>
    /// <param name="layersToNodes">List of all nodes</param>
    /// <param name="depth">Current depth</param>
    /// <param name="loadEditorParameters">Whether to load any custom editor parameters.</param>
    /// <param name="nodeViewModel">Returned node view model.</param>
    /// <returns>Whether the node was created, otherwise if false, it was already created.</returns>
    private bool TryGetOrCreateGraphNode(FSMNode node, List<List<FSMNode>> layersToNodes, ref int depth, bool loadEditorParameters, out FsmNodeViewModel nodeViewModel)
    {
        if (_processedNodes.Contains(node.Guid))
        {
            nodeViewModel = _guidToNodeVm[node.Guid];
            return false;
        }

        _processedNodes.Add(node.Guid);

        FsmNodeViewModel graphNode = CreateNodeViewModel(node, depth * 400, 0, loadEditorParameters);
        AddNode(graphNode);

        if (node.ChildLayerId != -1) // New layer
        {
            int depth_ = depth + 1;
            TryGetOrCreateGraphNode(node.Children[0], layersToNodes, ref depth_, loadEditorParameters, out FsmNodeViewModel subLayerNode);

            FsmConnectionViewModel connection = new()
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

            if (!LayerGroups.TryGetValue(node.ChildLayerId, out FsmGroupNodeViewModel? groupNode))
            {
                groupNode = new FsmGroupNodeViewModel()
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


        for (int i = 0; i < node.EndTransitions.Count; i++)
        {
            Transition trans = node.EndTransitions[i];
            AddTransition(trans, node, layersToNodes, depth, graphNode, i, loadEditorParameters: loadEditorParameters);
        }

        for (int i = 0; i < node.FailedTransitions.Count; i++)
        {
            Transition trans = node.FailedTransitions[i];
            AddTransition(trans, node, layersToNodes, depth, graphNode, i, loadEditorParameters: loadEditorParameters);
        }

        for (int i = 0; i < node.RegularTransitions.Count; i++)
        {
            Transition trans = node.RegularTransitions[i];
            AddTransition(trans, node, layersToNodes, depth, graphNode, i, loadEditorParameters: loadEditorParameters);
        }

        for (int i = 0; i < node.OverrideTransitions.Count; i++)
        {
            Transition trans = node.OverrideTransitions[i];
            AddTransition(trans, node, layersToNodes, depth, graphNode, i, true, loadEditorParameters: loadEditorParameters);
        }

        nodeViewModel = graphNode;

        // Possible orphan nodes
        foreach (var subNode in node.Children)
        {
            if (!_processedNodes.Contains(subNode.Guid))
            {
                TryGetOrCreateGraphNode(subNode, layersToNodes, ref depth, loadEditorParameters, out _);
            }
        }
        return true;
    }

    public void AddNode(FsmNodeViewModel node)
    {
        if (_rootNode is null)
        {
            _rootNode = node;
            node.IsLayerRootNode = true;
            node.UpdateVisuals();
        }

        RegisterFsmElementGuid(node.Guid, node);
        Nodes.Add(node);

        foreach (NodeComponentViewModel componentVM in node.Components)
        {
            RegisterFsmElementGuid(componentVM.Component.Guid, componentVM.Component);
        }
    }

    /// <summary>
    /// Marks the fsm as having quest context
    /// </summary>
    public void SetQuestContext(uint questId, uint progressIndex, ulong progressHash)
    {
        _questContext = new QuestContext(questId >> 20, questId >> 12 & 0xFF, questId & 0xFFF, progressIndex, progressHash);
    }

    /// <summary>
    /// Marks the fsm as having quest context
    /// </summary>
    public void SetQuestContext(uint category, uint subCategory, uint index, uint progressIndex, ulong progressHash)
    {
        _questContext = new QuestContext(category, subCategory, index, progressIndex, progressHash);
    }

    public QuestContext? GetQuestContext() => _questContext;

    public bool HasQuestContext() => _questContext is not null;

    /// <summary>
    /// Registers a graph component (node, bt component) to the guid mapping.
    /// </summary>
    /// <param name="guid"></param>
    /// <param name="elem"></param>
    public void RegisterFsmElementGuid(uint guid, object elem)
    {
        _guidToElement.TryAdd(guid, elem);
    }

    /// <summary>
    /// Unregisters a graph component from the guid mapping.
    /// </summary>
    /// <param name="guid"></param>
    public void UnregisterFsmElementGuid(uint guid)
    {
        _guidToElement.Remove(guid);
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
    private void AddTransition(Transition trans, FSMNode sourceNode, List<List<FSMNode>> layersToNodes, int depth, FsmNodeViewModel sourceNodeVm, int i,
        bool isOverrideTransition = false, bool loadEditorParameters = false)
    {
        FSMNode? toFsmNode = sourceNode.Children.FirstOrDefault(e => e.Guid == trans.FromNodeGuid);

        // This is kinda weird, we're gonna use the full node list here, but this shouldn't ever be needed - at best the parent node is used
        foreach (List<FSMNode> layer in layersToNodes)
        {
            foreach (FSMNode node in layer)
            {
                if (node.Guid == trans.FromNodeGuid)
                {
                    toFsmNode ??= node;
                    break;
                }
            }
        }

        FsmNodeViewModel toNode;
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
                _logger?.LogWarning("A transition refers to node {}, but it does not exist in node list", trans.FromNodeGuid);
                return;
            }
        }

        // regular node.
        toNode = CreateNodeViewModel(toFsmNode, (depth + 1) * 400, i * 200, loadEditorParameters: loadEditorParameters);
        if (trans.IsEndTransition)
        {
            toNode.Title = "END";
            toNode.IsEndNode = true;
            toNode.UpdateVisuals();
        }

        if (!_processedNodes.Contains(toFsmNode.Guid))
        {
            int depth_ = depth + 1;
            TryGetOrCreateGraphNode(toFsmNode, layersToNodes, ref depth_, loadEditorParameters: loadEditorParameters, out _);
        }

        FsmConnectionViewModel? connection;
        if (trans.ToNodeGuid == trans.FromNodeGuid)
            connection = Connections.FirstOrDefault(e => (e.Source == sourceNodeVm || e.Source == toNode) &&
                                                         (e.Target == sourceNodeVm || e.Target == toNode));
        else
            connection = Connections.FirstOrDefault(e => (e.Source == sourceNodeVm || e.Source == toNode) &&
                                                          (e.Target == sourceNodeVm || e.Target == toNode) && e.Source != e.Target);

        if (connection is null)
        {
            connection = new FsmConnectionViewModel
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

        if (trans.ConditionComponents.Count == 1)
        {
            // There's always a default operator group in a transition, add the condition to it
            transition.ConditionOperatorGroups[0].AddElement(new TransitionConditionViewModel(trans.ConditionComponents[0])
            {
                Title = trans.ConditionComponents[0].ComponentName,
                IsFalse = trans.ConditionComponents[0].IsReverseSuccess,
            });
        }
        else if (trans.ConditionComponents.Count >= 2)
        {
            transition.ConditionOperatorGroups.Clear();

            TransitionConditionElementViewModelBase root = BuildConditionTree(trans, connection);
            if (root is TransitionConditionOpViewModel rootOp)
            {
                rootOp.Operand = TransitionOperandType.None;
                transition.ConditionOperatorGroups.Add(rootOp);
            }
        }

        connection.UpdateConnection();
    }

    /// <summary>
    /// Builds condition operator group tree for a transition.
    /// </summary>
    /// <param name="fsmTransition"></param>
    /// <param name="connection"></param>
    /// <returns></returns>
    private static TransitionConditionElementViewModelBase BuildConditionTree(Transition fsmTransition, FsmConnectionViewModel connection)
    {
        int numComponents = fsmTransition.ConditionComponents.Count;
        if (numComponents == 1)
        {
            return TransitionConditionViewModel.FromConditionComponent(fsmTransition.ConditionComponents[0]);
        }
        else
        {
            var nodes = new TransitionConditionElementViewModelBase[numComponents];
            for (int i = 0; i < numComponents; i++)
                nodes[i] = TransitionConditionViewModel.FromConditionComponent(fsmTransition.ConditionComponents[i]);

            return BuildRange(0, numComponents - 1);

            TransitionConditionElementViewModelBase BuildRange(int start, int end)
            {
                if (start == end)
                    return nodes[start];

                int highestPriority = int.MinValue;
                for (int i = start; i < end; i++)
                    highestPriority = Math.Max(highestPriority, fsmTransition.TransitionParams[i].Priority);

                var splitIndices = new List<int>();
                for (int i = start; i < end; i++)
                {
                    if (fsmTransition.TransitionParams[i].Priority == highestPriority)
                        splitIndices.Add(i);
                }

                if (splitIndices.Count == 0)
                    throw new InvalidOperationException();

                var firstParam = fsmTransition.TransitionParams[splitIndices[0]];
                var operandType = firstParam.IsAndCondition ? TransitionOperandType.AND : TransitionOperandType.OR;

                var op = new TransitionConditionOpViewModel(connection, operatorType: operandType);

                int sectionStart = start;
                foreach (int split in splitIndices)
                {
                    op.AddElement(BuildRange(sectionStart, split));
                    sectionStart = split + 1;
                }

                op.AddElement(BuildRange(sectionStart, end));

                return op;
            }
        }
    }

    private readonly Dictionary<uint, FsmNodeViewModel> _guidToNodeVm = [];
    private readonly HashSet<uint> _processedNodes = [];

    private FSMState BuildTreeFromCurrentGraph()
    {
        var state = new FSMState()
        {
            EditorSettings =
            {
                ViewportLocation = new Vector2((float)ViewportLocation.X, (float)ViewportLocation.Y),
                ViewportSize = new Vector2((float)ViewportSize.Width, (float)ViewportSize.Height),
                ViewportZoom = (float)ViewportZoom,
            }
        };

        Dictionary<uint, FSMNode> processedNodes = [];

        var rootNode = CreateFSMNodeFromViewModel(_rootNode);
        processedNodes.TryAdd(rootNode.Guid, rootNode);
        state.Layers.Add(0, [rootNode]);

        BuildFSMNode(state, _rootNode, rootNode, processedNodes);

        var savedNodes = state.Layers.SelectMany(e => e.Value).ToFrozenDictionary(e => e.Guid, e => e);
        foreach (FsmNodeViewModelBase node in Nodes)
        {
            if (!state.Layers.TryGetValue(node.LayerIndex, out List<FSMNode>? layerNodes))
            {
                layerNodes = [];
                state.Layers[node.LayerIndex] = layerNodes;
            }

            if (node is FsmGroupNodeViewModel layerNode)
            {

            }
            else if (node is FsmNodeViewModel fsmNode)
            {
                if (fsmNode.IsEndNode)
                    continue;

                // Check for potential orphan nodes
                if (!savedNodes.ContainsKey(node.Guid))
                {
                    FSMNode? orphanNode = CreateFSMNodeFromViewModel(fsmNode);
                    processedNodes.TryAdd(orphanNode.Guid, orphanNode);
                    layerNodes.Add(orphanNode);

                    BuildFSMNode(state, fsmNode, orphanNode, processedNodes);

                    state.HasOrphanNodes = true;
                    _logger?.LogWarning("FSM has at least one orphan/disconnected node {name} ({guid}).", node.Title, node.Guid);
                }
            }
        }

        return state;
    }

    private void BuildFSMNode(FSMState fsmState, FsmNodeViewModel nodeVM, FSMNode fsmNode, Dictionary<uint, FSMNode> processedNodes)
    {
        fsmNode.FsmFolderName = nodeVM.FsmFolderName;
        fsmNode.FsmName = nodeVM.FsmName;
        fsmNode.NameHash = nodeVM.NameHash;

        foreach (NodeComponentViewModel componentViewModel in nodeVM.Components)
            fsmNode.ExecutionComponents.Add(componentViewModel.Component);

        foreach (var transition in nodeVM.Transitions)
        {
            FsmNodeViewModel sourceNvm = (FsmNodeViewModel)_guidToElement[transition.Source.Guid];
            FsmNodeViewModel targetNvm = (FsmNodeViewModel)_guidToElement[transition.Target.Guid];

            // Layer to layer transitions don't emit transition nodes, so only do same layer.
            if (transition.Source.LayerIndex == transition.Target.LayerIndex)
            {
                Transition trans = BuildFSMTransition(processedNodes, transition, fsmNode);
                if (transition.IsOverrideTransition)
                {
                    trans.ToNodeGuid = 0;
                    fsmNode.AddOverrideTransition(trans);
                }
                else
                {
                    fsmNode.AddTransition(trans);
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
            bool shouldProcessTarget = false;
            if (!processedNodes.TryGetValue(transition.Target.Guid, out FSMNode? targetFsmNode))
            {
                targetFsmNode = CreateFSMNodeFromViewModel(transition.Target);
                processedNodes.Add(targetFsmNode.Guid, targetFsmNode);
                shouldProcessTarget = true;
            }

            // Create or fetch new layer if needed.
            if (!fsmState.Layers.TryGetValue(transition.Target.LayerIndex, out List<FSMNode>? nodesForLayer))
            {
                nodesForLayer = [];
                fsmState.Layers.Add(transition.Target.LayerIndex, nodesForLayer);
            }

            // Ensure to link up layer transitions as child nodes
            if (transition.Source.LayerIndex != transition.Target.LayerIndex)
            {
                FsmGroupNodeViewModel layerGroupVm = LayerGroups[transition.Target.LayerIndex];
                sourceFsmNode.ChildLayerId = transition.Target.LayerIndex;
                sourceFsmNode.ChildLayerName = layerGroupVm.Title;

                var rect = layerGroupVm.BoundaryBox;
                sourceFsmNode.ChildLayerBoundaryBox = new Vector4((float)rect.X, (float)rect.Y, (float)rect.Width, (float)rect.Height);

                // Counts as one child.
                sourceFsmNode.Children.Add(targetFsmNode);
            }

            // Reminder, end nodes aren't actually emitted as a node on the fsm file.
            if (shouldProcessTarget)
            {
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

    private static FSMNode CreateFSMNodeFromViewModel(FsmNodeViewModel nodeVM)
    {
        return new FSMNode(nodeVM.Guid)
        {
            NameHash = CRC32.crc32_0x77073096(nodeVM.Title ?? "FSMNode"),
            Name = nodeVM.Title,
            BoundaryBox = new Vector4((float)nodeVM.Location.X, (float)nodeVM.Location.Y, (float)nodeVM.Size.Width, (float)nodeVM.Size.Height),
            IsBranch = nodeVM.IsBranch,
        };
    }

    private static Transition BuildFSMTransition(Dictionary<uint, FSMNode> processedNodes, TransitionViewModel transition, FSMNode node)
    {
        Transition fsmTransition = new(transition.Source.Guid, transition.Target.Guid);

        var root = transition.ConditionOperatorGroups[0];
        if (root.Elements.Count > 0)
        {
            root.FlattenConditionTree(out List<ConditionComponent> components, out List<Transition.TransitionParam> operators);

            for (int i = 0; i < components.Count; i++)
            {
                ConditionComponent component = components[i];

                fsmTransition.ConditionComponents.Add(component);
                fsmTransition.ConditionGuids.Add(component.Guid);
            }

            for (int i = 0; i < operators.Count; i++)
            {
                Transition.TransitionParam @operator = operators[i];
                fsmTransition.TransitionParams.Add(@operator);
            }
        }

        return fsmTransition;
    }
}
