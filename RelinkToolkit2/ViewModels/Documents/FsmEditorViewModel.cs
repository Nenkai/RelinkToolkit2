
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Rendering.Composition;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using Dock.Model.Core;
using Dock.Model.Mvvm.Controls;

using GBFRDataTools.FSM;
using GBFRDataTools.FSM.BehaviorTree;
using GBFRDataTools.FSM.Components;
using GBFRDataTools.FSM.Components.Actions.Quest;
using GBFRDataTools.FSM.Entities;

using Microsoft.Msagl.Core.Layout;

using Nodify;

using RelinkToolkit2.Messages.Fsm;
using RelinkToolkit2.Messages.IO;
using RelinkToolkit2.ViewModels.Fsm;
using RelinkToolkit2.ViewModels.Fsm.TransitionComponents;
using RelinkToolkit2.ViewModels.Menu;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

//using MsBox.Avalonia;

namespace RelinkToolkit2.ViewModels.Documents;

public partial class FsmEditorViewModel : EditorDocumentBase
{
    /// <summary>
    /// Lookup table for all elements with a guid (nodes, or components).
    /// </summary>
    private readonly Dictionary<uint, object> _guidToFsmElement = [];

    /// <summary>
    /// All displayed nodes in the graph (including groups).
    /// </summary>
    public ObservableCollection<NodeViewModelBase> Nodes { get; } = [];

    /// <summary>
    /// Connections in the graph.
    /// </summary>
    public ObservableCollection<GraphConnectionViewModel> Connections { get; } = [];

    /// <summary>
    /// All layers (as a group) on the graph. Root layer is not included.
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
    private Point _contextMenuLocation;

    public DocumentsViewModel? Documents { get; set; }
    private string? _currentFsmName;

    private NodeViewModel _rootNode;

    /// <summary>
    /// Menu items for the main context menu (add nodes, etc).
    /// </summary>
    public ObservableCollection<MenuItemViewModel> EditorContextMenuItems { get; set; } = [];

    public Point MouseLocation { get; set; }

    public FsmEditorViewModel()
    {
        if (Design.IsDesignMode)
        {
            var node = new NodeViewModel()
            {
                ParentEditor = this,
                Title = "Test2",
                Guid = 123456789,
                FsmSource = "playerai/test",
                BorderBrush = Brushes.Red,
            };

            node.Components.Add(new NodeComponentViewModel(node)
            {
                Component = new CallStaffRoll(),
            });
            node.Components.Add(new NodeComponentViewModel(node)
            {
                Component = new CallSe(),
            });
            AddNode(node);

            var node2 = new NodeViewModel()
            {
                ParentEditor = this,
                Title = "Test2",
                Guid = 12345678,
                FsmSource = "playerai/test",
                BorderBrush = Brushes.Red,
                Location = new Point(100, 0),
            };
            AddNode(node2);

            Connections.Add(new GraphConnectionViewModel()
            {
                Source = node,
                Target = node2,
            });

            Connections.Add(new GraphConnectionViewModel()
            {
                Source = node2,
                Target = node2,
            });
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
            IconKind = "Material.CogBox",
            Enabled = true,
            Command = new RelayCommand(AddNewNode),
        });
    }

    public override void RegisterMessageListeners()
    {
        // Fired when saving from top menu
        WeakReferenceMessenger.Default.Register<GraphFileSaveRequestMessage>(this, (recipient, message) =>
        {
            SaveFSMFile(message.Value);
        });

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

    // Document closing
    public override bool OnClose()
    {
        Documents?.Remove(Id);
        return base.OnClose();
    }

    private void ResetGraph()
    {
        _processedNodes.Clear();
        _guidToFsmElement.Clear();
        Nodes.Clear();
        Connections.Clear();
        LayerGroups.Clear();
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
        WeakReferenceMessenger.Default.Send(new EditConnectionRequest(value));
    }

    /// <summary>
    /// Saves the graph as a FSM file.
    /// </summary>
    /// <param name="fileName"></param>
    public void SaveFSMFile(string fileName)
    {
        FSMBuildState buildState = BuildTreeFromCurrentGraph();

        using Stream stream = File.Create(fileName);
        var builder = new FSMSerializer(buildState);
        builder.WriteJson(stream);
    }

    /// <summary>
    /// Inits the graph from the loaded nodes.
    /// </summary>
    public void InitGraph(string fsmName, FSMParser fsmParser)
    {
        _currentFsmName = fsmName;
        ResetGraph();

        LayerGroups.Add(0, new GroupNodeViewModel() { ParentEditor = this, LayerIndex = 0 });

        try
        {

            if (fsmParser?.RootNode is null)
            {
                foreach (var group in fsmParser.NonEmptyLayersToNodes)
                {
                    if (group.Count == 0)
                        continue;

                    int depth = 0;
                    CreateViewModelFromFSMNode(group[0], fsmParser.AllNodes, ref depth);
                }
            }
            else
            {
                int depth = 0;
                NodeViewModel? root = CreateViewModelFromFSMNode(fsmParser.RootNode, fsmParser.AllNodes, ref depth);
                if (root is not null)
                {
                    root.BorderBrush = Brushes.White;
                    root.CornerRadius = new CornerRadius(0);
                    root.IsLayerRootNode = true;
                    _rootNode = root;
                }
            }
        }
        catch (Exception ex)
        {
            //MessageBoxManager.GetMessageBoxStandard("Oops", $"Errored: {ex.Message}");
            WeakReferenceMessenger.Default.Send(new FSMFileLoadStateChangedMessage(false));
            return;
        }

        WeakReferenceMessenger.Default.Send(new FSMFileLoadStateChangedMessage(true));
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

        AddNode(node);
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
                connection.Source.RemoveAllTransitionsWithGuid(nodeVm.Guid);
                connection.Target.RemoveAllTransitionsWithGuid(nodeVm.Guid);
            }
        }

        Nodes.Remove(nodeVm);
        UnregisterFsmElementGuid(nodeVm.Guid);
    }

    /// <summary>
    /// Creates a graph node from the specified FSM node (model)
    /// </summary>
    /// <param name="node"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    private NodeViewModel GetNodeViewModel(FSMNode node, int x, int y)
    {
        if (_guidToNodeVm.TryGetValue(node.Guid, out NodeViewModel? nodeViewModel))
            return nodeViewModel;

        string title = NodeNameStore.TryGetNameForNode(_currentFsmName, node.Guid, node.NameHash);
        if (string.IsNullOrEmpty(title))
            title = $"{node.Guid}";

        nodeViewModel = new NodeViewModel()
        {
            ParentEditor = this,
            Guid = node.Guid,
            Title = title,
            Location = new Point(x, y),
            LayerIndex = node.LayerIndex,
            NameHash = node.NameHash,
            FsmFolderName = node.FsmFolderName,
            FsmName = node.FsmName,
        };

        if (!string.IsNullOrEmpty(node.FsmFolderName))
        {
            nodeViewModel.FsmSource = $"{node.FsmFolderName}/{node.FsmFolderName}_{node.FsmName}";
        }

        if (node.ExecutionComponents.Count == 0)
            nodeViewModel.BorderBrush = Brushes.Black;

        foreach (BehaviorTreeComponent component in node.ExecutionComponents)
        {
            nodeViewModel.Components.Add(new NodeComponentViewModel(nodeViewModel)
            {
                Name = component.ToString(),
                Component = component,
            });
        }

        _guidToNodeVm.Add(node.Guid, nodeViewModel);

        return nodeViewModel;
    }

    [RelayCommand]
    public void OnConnectionCompleted(NodeViewModel? target)
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
        bool canConnect = connection is null && !sourceNode.IsEndNode;
        bool canOverrideTransition = canConnect && sourceNode.IsLayerRootNode; // Override transitions are only allowed in layer starts

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
                Command = new RelayCommand(ConnectNodes)
            },
            new MenuItemViewModel()
            {
                Header = "Override Transition (only in layer starts)",
                Enabled = canOverrideTransition,
                IconKind = "Material.RayStartArrow",
                IconBrush = GraphColors.OverrideTransition,
            },
        ];

        var flyout = new MenuFlyout();
        flyout.ItemsSource = items;
        flyout.ShowAt(message.Response, showAtPointer: true);
    }

    [RelayCommand]
    public void OnItemsDragCompleted(object data)
    {
        foreach (NodeViewModelBase node in SelectedNodes)
        {
            if (node is GroupNodeViewModel)
                continue;

            // Allow 1/4 on each edge

            double bboxStartX = node.Location.X + (node.Size.Width * 0.25);
            double bboxEndX = node.Location.X + (node.Size.Width * 0.75);

            double bboxStartY = node.Location.Y + (node.Size.Height * 0.25);
            double bboxEndY = node.Location.Y + (node.Size.Height * 0.75);

            int i = 0;
            foreach (var layer in LayerGroups.Values)
            { 
                double layerEndX = layer.Location.X + layer.Size.Width;
                double layerEndY = layer.Location.Y + layer.Size.Height;

                if (bboxStartX >= layer.Location.X && bboxEndX <= layerEndX &&
                    bboxStartY >= layer.Location.Y && bboxEndY <= layerEndY)
                {
                    node.LayerIndex = layer.LayerIndex;
                    break;
                }

                if (i == LayerGroups.Count - 1)
                    node.LayerIndex = 0;

                i++;
            }
        }
    }

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
                ArrowColor = GraphColors.NormalTransition,
            };

            Connections.Add(existingConnection);
        }

        var transition = new TransitionViewModel(existingConnection)
        {
            Source = PendingConnection.Source,
            Target = PendingConnection.Target
        };
        PendingConnection.Source.Transitions.Add(transition);
        if (PendingConnection.Source == PendingConnection.Target)
            PendingConnection.Target.Transitions.Add(transition);

        existingConnection?.Transitions.Add(transition);

        WeakReferenceMessenger.Default.Send(new EditConnectionRequest(existingConnection!));
    }

    /// <summary>
    /// Creates a view model node for the specified fsm node.
    /// </summary>
    /// <param name="node"></param>
    /// <param name="nodeList"></param>
    /// <param name="depth"></param>
    /// <returns></returns>
    private NodeViewModel? CreateViewModelFromFSMNode(FSMNode node, List<FSMNode> nodeList, ref int depth)
    {
        if (_processedNodes.Contains(node.Guid))
            return null;

        _processedNodes.Add(node.Guid);

        NodeViewModel graphNode = GetNodeViewModel(node, depth * 400, 0);
        AddNode(graphNode);

        if (node.ChildLayerId != -1) // New layer
        {
            graphNode.BorderBrush = Brushes.DarkBlue;

            int depth_ = depth + 1;
            NodeViewModel? subLayerNode = CreateViewModelFromFSMNode(node.Children[0], nodeList, ref depth_);
            if (subLayerNode is not null)
            {
                GraphConnectionViewModel connection = new()
                {
                    Source = graphNode,
                    Target = subLayerNode,
                };

                graphNode.Title += $" (to Layer{node.ChildLayerId})";
                graphNode.Transitions.Add(new TransitionViewModel(connection)
                {
                    Source = graphNode,
                    Target = subLayerNode,
                });

                connection.StrokeDashArray = [1];
                connection.ArrowHeadEnds = ArrowHeadEnds.None;
                connection.ArrowColor = Brushes.DimGray;
                Connections.Add(connection);

                if (!LayerGroups.TryGetValue(node.ChildLayerId, out GroupNodeViewModel groupNode)!)
                {
                    LayerGroups.Add(node.ChildLayerId, new GroupNodeViewModel()
                    {
                        ParentEditor = this,
                        LayerIndex = node.ChildLayerId,
                        Title = $"Layer {node.ChildLayerId}",
                    });
                    subLayerNode.IsLayerRootNode = true;
                }
            }
        }

        for (int i = 0; i < node.RegularTransitions.Count; i++)
        {
            Transition trans = node.RegularTransitions[i];
            AddTransition(trans, node, nodeList, depth, graphNode, i);
        }

        for (int i = 0; i < node.OverrideTransitions.Count; i++)
        {
            Transition trans = node.OverrideTransitions[i];
            AddTransition(trans, node, nodeList, depth, graphNode, i, true);
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

    public void RegisterFsmElementGuid(uint guid, object elem)
    {
        _guidToFsmElement.TryAdd(guid, elem);
    }

    public void UnregisterFsmElementGuid(uint guid)
    {
        _guidToFsmElement.Remove(guid);
    }

    /// <summary>
    /// Creates a transition.
    /// </summary>
    /// <param name="sourceNode"></param>
    /// <param name="allNodesList"></param>
    /// <param name="depth"></param>
    /// <param name="sourceNodeVm"></param>
    /// <param name="i"></param>
    /// <param name="trans"></param>
    /// <param name="isOverrideTransition"></param>
    private void AddTransition(Transition trans, FSMNode sourceNode, List<FSMNode> allNodesList, int depth, NodeViewModel sourceNodeVm, int i, bool isOverrideTransition = false)
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
            }
            else
            {
                // This branch is invalid?
                Debug.WriteLine($"WARN: A transition refers to node {trans.FromNodeGuid}, but it does not exist in node list");
                return;
            }
        }

        // regular node.
        toNode = GetNodeViewModel(toFsmNode, (depth + 1) * 400, i * 200);
        if (trans.IsEndTransition)
        {
            toNode.Title = "END";
            toNode.BorderBrush = GraphColors.EndingNode;
            toNode.IsEndNode = true;
        }

        if (!_processedNodes.Contains(toFsmNode.Guid))
        {
            int depth_ = depth + 1;
            CreateViewModelFromFSMNode(toFsmNode, allNodesList, ref depth_);
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

    private FSMBuildState BuildTreeFromCurrentGraph()
    {
        var state = new FSMBuildState();

        Dictionary<uint, FSMNode> processedNodes = [];

        var rootNode = new FSMNode(_rootNode.Guid);
        processedNodes.TryAdd(rootNode.Guid, rootNode);
        state.Layers.Add(0, [rootNode]);

        BuildFSMNode(state, _rootNode, rootNode, rootNode, processedNodes);
        return state;
    }

    private void BuildFSMNode(FSMBuildState fsmState, NodeViewModel nodeVM, FSMNode fsmNode, FSMNode currentLayerRootNode, Dictionary<uint, FSMNode> processedNodes)
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
                    trans.IsEndTransition = true;
            }

            // Process source
            if (!processedNodes.TryGetValue(transition.Source.Guid, out FSMNode? sourceFsmNode))
            {
                sourceFsmNode = new FSMNode(transition.Source.Guid);
                processedNodes.Add(sourceFsmNode.Guid, sourceFsmNode);

                BuildFSMNode(fsmState, sourceNvm, sourceFsmNode, currentLayerRootNode, processedNodes);
            }

            // Process target, and layer transitions if needed.
            if (!processedNodes.TryGetValue(transition.Target.Guid, out FSMNode? targetFsmNode))
            {
                targetFsmNode = new FSMNode(transition.Target.Guid);
                processedNodes.Add(targetFsmNode.Guid, targetFsmNode);

                if (!currentLayerRootNode.Children.Contains(targetFsmNode) && !targetNvm.IsEndNode)
                    currentLayerRootNode.Children.Add(targetFsmNode);

                if (!fsmState.Layers.TryGetValue(transition.Target.LayerIndex, out List<FSMNode>? nodesForLayer))
                {
                    nodesForLayer = [];
                    fsmState.Layers.Add(transition.Target.LayerIndex, nodesForLayer);
                }

                if (transition.Source.LayerIndex != transition.Target.LayerIndex)
                {
                    sourceFsmNode.ChildLayerId = transition.Target.LayerIndex;
                    currentLayerRootNode = targetFsmNode;
                }

                // Reminder, end nodes aren't actually emitted as a node on the fsm file.
                if (!targetNvm.IsEndNode)
                    fsmState.Layers[transition.Target.LayerIndex].Add(targetFsmNode);

                BuildFSMNode(fsmState, targetNvm, targetFsmNode, currentLayerRootNode, processedNodes);
            }
        }

        fsmNode.TailIndexOfChildNodeGuids = 0;
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
