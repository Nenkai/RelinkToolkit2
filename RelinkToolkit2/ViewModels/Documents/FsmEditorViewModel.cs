
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.ComponentModel;
using System.IO;

using Avalonia;
using Avalonia.Media;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using Nodify;

using GBFRDataTools.FSM.Entities;
using GBFRDataTools.FSM;

using Dock.Model.Core;
using Dock.Model.Mvvm.Controls;

using RelinkToolkit2.Messages.IO;
using RelinkToolkit2.Messages.Fsm;
using RelinkToolkit2.ViewModels.Fsm;
using GBFRDataTools.FSM.Components.Actions.Quest;
using RelinkToolkit2.ViewModels.Menu;
using Avalonia.Collections;
using Microsoft.Msagl.Core.Layout;

//using MsBox.Avalonia;

namespace RelinkToolkit2.ViewModels.Documents;

public partial class FsmEditorViewModel : Document
{
    public FSMParser? FSM { get; set; }

    /// <summary>
    /// All nodes in the graph (including groups).
    /// </summary>
    public ObservableCollection<NodeViewModelBase> Nodes { get; } = [];

    /// <summary>
    /// Connections in the graph.
    /// </summary>
    public ObservableCollection<ConnectionViewModel> Connections { get; } = [];

    public PendingConnectionViewModel PendingConnection { get; set; } = new();

    [ObservableProperty]
    private NodeViewModel? _selectedNode;

    [ObservableProperty]
    private ConnectionViewModel? _selectedConnection;

    [ObservableProperty]
    private Point _contextMenuLocation;

    public DocumentsViewModel Documents { get; set; }

    public FsmEditorViewModel()
    {
        WeakReferenceMessenger.Default.Register<FileSaveRequestMessage>(this, (recipient, message) =>
        {
            SaveFSMFile(message.Value);
        });

        WeakReferenceMessenger.Default.Register<NodeGraphSelectionChangeRequest>(this, (recipient, message) =>
        {
            SelectedNode = message.Node;
            message.Reply(true);
        });

        WeakReferenceMessenger.Default.Register<DeleteNodeConnectionRequest>(this, (recipient, message) =>
        {
            bool result = Connections.Remove(message.Connection);
            message.Reply(result);
        });

        if (Design.IsDesignMode)
        {
            var node = new NodeViewModel()
            {
                Title = "Test",
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
            Nodes.Add(node);
        }
    }

    // Document closing
    public override bool OnClose()
    {
        Documents.Remove(Id);
        return base.OnClose();
    }

    /// <summary>
    /// Called by generated observable property <see cref="_selectedNode"/>
    /// </summary>
    /// <param name="value"></param>
    partial void OnSelectedNodeChanged(NodeViewModel? value)
    {

    }

    /// <summary>
    /// Caleld by generated observable property <see cref="_selectedConnection"/>
    /// </summary>
    /// <param name="value"></param>
    partial void OnSelectedConnectionChanged(ConnectionViewModel? value)
    {
        WeakReferenceMessenger.Default.Send(new ConnectionSelectionChangedMessage(value));
    }

    /// <summary>
    /// Saves the graph as a FSM file.
    /// </summary>
    /// <param name="fileName"></param>
    public void SaveFSMFile(string fileName)
    {
        FSMNode rootNode = BuildTreeFromCurrentGraph();

        using Stream stream = File.OpenWrite(fileName);
        var builder = new FSMSerializer(rootNode);
        builder.WriteJson(stream);
    }

    /// <summary>
    /// Inits the graph from the loaded nodes.
    /// </summary>
    public void InitGraph()
    {
        _processedNodes.Clear();
        Nodes.Clear();
        Connections.Clear();

        try
        {
            
            if (FSM.RootNode is null)
            {
                foreach (var group in FSM.GroupsToNodes)
                {
                    if (group.Count == 0)
                        continue;

                    int depth = 0;
                    CreateViewModelFromFSMNode(group[0], FSM.AllNodes, ref depth);
                }
            }
            else
            {
                int depth = 0;
                NodeViewModel? root = CreateViewModelFromFSMNode(FSM.RootNode, FSM.AllNodes, ref depth);
                if (root is not null)
                {
                    root.BorderBrush = Brushes.White;
                    root.CornerRadius = new CornerRadius(0);
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
        Nodes.Add(graphNode);

        if (node.ChildLayerId != -1) // New layer
        {
            graphNode.BorderBrush = Brushes.DarkBlue;

            int depth_ = depth + 1;
            NodeViewModel? subLayerNode = CreateViewModelFromFSMNode(node.Children[0], nodeList, ref depth_);
            if (subLayerNode is not null)
            {
                ConnectionViewModel connection = new()
                {
                    Source = graphNode,
                    Target = subLayerNode,
                };

                graphNode.Title += $" (Transition to Layer{node.ChildLayerId})";
                connection.StrokeDashArray = new AvaloniaList<double>() { 1 };
                connection.ArrowHeadEnds = ArrowHeadEnds.None;
                connection.ArrowColor = Brushes.DimGray; 
                Connections.Add(connection);
            }
        }

        for (int i = 0; i < node.BranchTransitions.Count; i++)
        {
            Transition trans = node.BranchTransitions[i];
            AddTransition(trans, node, nodeList, depth, graphNode, i);
        }
        
        for (int i = 0; i < node.LeafTransitions.Count; i++)
        {
            Transition trans = node.LeafTransitions[i];
            AddTransition(trans, node, nodeList, depth, graphNode, i, true);
        }

        return graphNode;
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
    /// <param name="isLeaf"></param>
    private void AddTransition(Transition trans, FSMNode sourceNode, List<FSMNode> allNodesList, int depth, NodeViewModel sourceNodeVm, int i, bool isLeaf = false)
    {
        FSMNode? toFsmNode = sourceNode.Children.FirstOrDefault(e => e.Guid == trans.FromNodeGuid);

        // This is kinda weird, we're gonna use the full node list here, but this shouldn't ever be needed - at best the parent node is used
        toFsmNode ??= allNodesList.FirstOrDefault(e => e.Guid == trans.FromNodeGuid);

        NodeViewModel toNode;
        if (toFsmNode is null)
        {
            if (trans.IsEndTransition)
            {
                toFsmNode = new FSMNode()
                {
                    Guid = trans.FromNodeGuid,
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
            toNode.Title = $"END ({toNode.Guid})";
            toNode.BorderBrush = GraphColors.EndingNode;
        }

        if (!_processedNodes.Contains(toFsmNode.Guid))
        {
            int depth_ = depth + 1;
            CreateViewModelFromFSMNode(toFsmNode, allNodesList, ref depth_);
        }


        ConnectionViewModel? connection = Connections.FirstOrDefault(e => (e.Source == sourceNodeVm || e.Source == toNode) &&
                                                                          (e.Target == sourceNodeVm || e.Target == toNode));
        
        if (connection is null)
        {
            connection = new ConnectionViewModel()
            {
                Source = sourceNodeVm,
                Target = toNode,
                ArrowColor = isLeaf ? GraphColors.UnkTransition : GraphColors.NormalTransition,
            };

            Connections.Add(connection);
        }
        else
        {
            connection.ArrowHeadEnds = ArrowHeadEnds.Both;
        }

        TransitionViewModel transition = new(connection)
        {
            Source = sourceNodeVm,
            Target = toNode,
        };

        connection.Transitions.Add(transition);

        if (trans.ConditionComponents.Count != 0)
        {
            for (int j = 0; j < trans.ConditionComponents.Count; j++)
            {
                transition.ConditionComponents.Add(new TransitionConditionComponentViewModel(trans.ConditionComponents[j])
                {
                    Title = trans.ConditionComponents[j].ComponentName,
                    IsFalse = trans.ConditionComponents[j].IsReverseSuccess,
                });
            }

            connection.UpdateConnection();
        }
    }

    private readonly Dictionary<int, NodeViewModel> _guidToNodeVm = [];
    private readonly HashSet<int> _processedNodes = [];

    private NodeViewModel GetNodeViewModel(FSMNode node, int x, int y)
    {
        if (_guidToNodeVm.TryGetValue(node.Guid, out NodeViewModel? nodeViewModel))
            return nodeViewModel;

        nodeViewModel = new NodeViewModel()
        {
            Guid = node.Guid,
            Title = $"{node.Guid}",
            Location = new Point(x, y),
            LayerIndex = node.LayerIndex,
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

    public FSMNode BuildTreeFromCurrentGraph()
    {
        Dictionary<int, FSMNode> fsmNodes = [];

        foreach (ConnectionViewModel connection in Connections)
        {
            foreach (TransitionViewModel transition in connection.Transitions)
            {
                if (!fsmNodes.TryGetValue(connection.Source.Guid, out FSMNode? sourceFsmNode))
                {
                    sourceFsmNode = new FSMNode();
                    sourceFsmNode.Guid = connection.Source.Guid;

                    Transition fsmTransition = new(transition.Source.Guid, connection.Target.Guid);
                    foreach (TransitionConditionComponentViewModel conditionComponent in transition.ConditionComponents)
                        fsmTransition.ConditionComponents.Add(conditionComponent.ConditionComponent);

                    foreach (NodeComponentViewModel componentViewModel in connection.Source.Components)
                        sourceFsmNode.ExecutionComponents.Add(componentViewModel.Component);

                    sourceFsmNode.BranchTransitions.Add(fsmTransition);

                    fsmNodes.Add(sourceFsmNode.Guid, sourceFsmNode);
                }

                if (!fsmNodes.TryGetValue(connection.Target.Guid, out FSMNode? targetFsmNode))
                {
                    targetFsmNode = new FSMNode();
                    targetFsmNode.Guid = connection.Target.Guid;

                    foreach (NodeComponentViewModel componentViewModel in connection.Target.Components)
                        targetFsmNode.ExecutionComponents.Add(componentViewModel.Component);

                    fsmNodes.Add(targetFsmNode.Guid, targetFsmNode);
                }

                if (sourceFsmNode.Children.Find(e => e.Guid == targetFsmNode.Guid) is null)
                    sourceFsmNode.Children.Add(targetFsmNode);
            }
        }

        var root = fsmNodes[FSM.RootNode.Guid];

        // Build tail index
        int idx = 0;
        int tailIndex = GetTailIndex(root);
        root.TailIndexOfChildNodeGuids = idx;
        return root;
    }

    public int GetTailIndex(FSMNode node)
    {
        int cnt = 0;
        for (int i = 0; i < node.Children.Count; i++)
        {
            cnt++;
            cnt += GetTailIndex(node.Children[i]);
        }
        return cnt;
    }

    [RelayCommand]
    public void OnConnectionCompleted(NodeViewModel? target)
    {
        if (target is null)
            return;

        // Bring up context menu
        // Not ideal and not very MVVM friendly to ask the view to respond, but no other way.
        var message = WeakReferenceMessenger.Default.Send(new GetNodeControlRequest(target));

        var sourceNode = PendingConnection.Source;
        var targetNode = PendingConnection.Target;

        bool canConnect = !Connections.Any(e => e.Transitions.Any(e => e.Source == sourceNode && e.Target == targetNode));
        ObservableCollection<MenuItemViewModel> items =
        [
            new MenuItemViewModel()
            {
                Header = "New Transition",
                FontWeight = FontWeight.Bold,
                IconKind = "Material.SwapHorizontal",
            },
            MenuItemViewModel.Separator,
            new MenuItemViewModel()
            {
                Header = "Normal Transition",
                Enabled = canConnect,
                IconKind = "Material.RayStartArrow",
                IconBrush = GraphColors.NormalTransition,
                Command = new RelayCommand(ConnectNormal)
            },
            new MenuItemViewModel()
            {
                Header = "Unknown Transition",
                Enabled = canConnect,
                IconKind = "Material.RayStartArrow",
                IconBrush = GraphColors.UnkTransition,
            },
        ];

        var flyout = new MenuFlyout();
        flyout.ItemsSource = items;
        flyout.ShowAt(message.Response, showAtPointer: true);
    }

    public void ConnectNormal()
    {
        if (PendingConnection?.Source is null || PendingConnection?.Target is null)
            return;

        ConnectionViewModel? existingConnection = Connections.FirstOrDefault(e => e.Source == PendingConnection.Source && e.Target == PendingConnection.Target ||
                                                                                  e.Target == PendingConnection.Source && e.Source == PendingConnection.Target);

        if (existingConnection is null)
        {
            existingConnection = new ConnectionViewModel()
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

        existingConnection?.Transitions.Add(transition);
    }
}
