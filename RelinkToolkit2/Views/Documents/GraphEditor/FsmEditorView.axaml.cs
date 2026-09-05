using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;

using CommunityToolkit.Mvvm.Messaging;

using DynamicData;

using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Layout.Layered;

using Nodify;

using RelinkToolkit2.Controls;
using RelinkToolkit2.Messages.Documents;
using RelinkToolkit2.Messages.Fsm;
using RelinkToolkit2.ViewModels.Documents.GraphEditor;
using RelinkToolkit2.ViewModels.Documents.GraphEditor.Nodes;
using RelinkToolkit2.ViewModels.Menu;
using RelinkToolkit2.Views.Search;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RelinkToolkit2.Views.Documents.GraphEditor;

public partial class FsmEditorView : UserControl,
    IRecipient<BringNodeIntoViewRequest>,
    IRecipient<CopyNodeGuidRequest>,
    IRecipient<DismissSearchComponentMenuRequest>,
    IRecipient<FsmComponentContextMenuRequest>,
    IRecipient<OpenContextMenuFlyoutAtNode>,
    IRecipient<OpenComponentSearchFlyoutRequest>
    
{
    private Flyout? _searchFlyout;

    public FsmEditorView()
    {
        InitializeComponent();
    }

    private void NodifyEditor_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        RegisterMessages();
        PerformAutomaticNodeLayouting();

        // Highlight first node if needed
        var editorVM = Editor.DataContext as FsmEditorViewModel;
        if (editorVM is not null)
        {
            var firstNode = editorVM.Nodes.Count > 0 ? editorVM.Nodes[0] : null;
            if (Editor.Tag is null && firstNode is not null)
            {
                Editor.BringIntoView(firstNode.Center, animated: false);
            }

            Editor.Tag = true;
        }
    }

    private void NodifyEditor_Unloaded_1(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        UnregisterMessages();
    }

    private void RegisterMessages()
    {
        WeakReferenceMessenger.Default.RegisterAll(this);
    }

    private void UnregisterMessages()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    /// <summary>
    /// Fired when a node is requested to be focused on.
    /// </summary>
    /// <param name="message"></param>
    public void Receive(BringNodeIntoViewRequest message)
    {
        if (DataContext is not FsmEditorViewModel editorVm)
            return;

        if (!editorVm.Nodes.Contains(message.Node))
            return;

        Editor.BringIntoView(message.Node.Center, animated: message.Animated);
        Editor.ZoomAtPosition(1.0f, message.Node.Center);
        message.Reply(true);
    }

    /// <summary>
    /// Fired when a flyout should be opened at the specified node
    /// </summary>
    /// <param name="message"></param>
    public void Receive(OpenContextMenuFlyoutAtNode message)
    {
        Control? control = Editor.ContainerFromItem(message.Node);
        if (control is null)
            return;

        var flyout = new ExtendableMenuFlyout
        {
            ItemsSource = message.Items
        };
        flyout.ShowAt(control, showAtPointer: true);
    }

    /// <summary>
    /// Fired when a node id should be copied
    /// </summary>
    /// <param name="message"></param>
    public async void Receive(CopyNodeGuidRequest message)
    {
        Control? control = Editor.ContainerFromItem(message.Node);
        if (control is null)
            return;

        var clipboard = TopLevel.GetTopLevel(control)?.Clipboard;
        if (clipboard is not null)
        {
            var dataObject = new DataTransfer();
            dataObject.Add(DataTransferItem.CreateText(message.Node.Guid.ToString()));
            await clipboard.SetDataAsync(dataObject);
        }
    }

    /// <summary>
    /// Fired when component search should be dismissed
    /// </summary>
    /// <param name="message"></param>
    public void Receive(DismissSearchComponentMenuRequest message)
    {
        _searchFlyout?.Hide();
        message.Reply(true);
    }

    /// <summary>
    /// Fired when a context menu is requested to be open for a node
    /// </summary>
    /// <param name="message"></param>
    public void Receive(FsmComponentContextMenuRequest message)
    {
        var editor = (FsmEditorViewModel)DataContext!;
        ObservableCollection<MenuItemViewModel> items = editor.CreateContextMenuForNode((FsmNodeViewModel)message.Component.Parent, message.Component);

        var flyout = new ExtendableMenuFlyout
        {
            ItemsSource = items
        };
        flyout.ShowAt(message.NodeView, showAtPointer: true);

        message.Reply(true);
    }

    /// <summary>
    /// Fired when a request to open a component search flyout is received
    /// </summary>
    /// <param name="message"></param>
    public void Receive(OpenComponentSearchFlyoutRequest message)
    {
        Control? control = Editor.ContainerFromItem(message.Node);
        if (control is null)
            return;

        _searchFlyout?.Hide();
        _searchFlyout ??= new Flyout { ShowMode = FlyoutShowMode.Transient, VerticalOffset = 4, OverlayDismissEventPassThrough = true };

        var content = new ComponentSearchView { DataContext = message.SearchViewModel };
        _searchFlyout.Content = content;
        _searchFlyout.ShowAt(control, showAtPointer: false);
    }

    /// <summary>
    /// Fired when nodes are loaded.
    /// Any automatic node layouting happens here.
    /// </summary>
    private void PerformAutomaticNodeLayouting()
    {
        FsmEditorViewModel editor = (FsmEditorViewModel)Editor.DataContext!;
        if (Editor.Items.Any() && !editor.IsLayouted)
        {
            // Nodes are now loaded. Proceed to create an automated layout
            // Ensure to update everything
            Editor.UpdateLayout();

            // Each layer will go into a different graph.
            // All root nodes will be grouped aswell for display purposes. They just won't actually be inserted into a group.
            ItemCollection items = Editor.Items;
            var groups = items.Where(e => e is FsmNodeViewModel)
                .Cast<FsmNodeViewModel>()
                .GroupBy(e => e.LayerIndex);

            Dictionary<int /* layerIndex */, GeometryGraph> graphPerLayer = [];
            foreach (var layer in groups)
            {
                GeometryGraph layerGraph = CreateGraphForLayer(layer);
                graphPerLayer.Add(layer.Key, layerGraph);
            }

            // Link nodes together. We won't link layers together yet as we need to compute how big they are.
            foreach (FsmConnectionViewModel connection in Editor.Connections)
            {
                FsmNodeViewModel srcFsmNode = (FsmNodeViewModel)connection.Source;
                FsmNodeViewModel dstFsmNode = (FsmNodeViewModel)connection.Target;

                if (srcFsmNode.LayerIndex != dstFsmNode.LayerIndex)
                    continue;

                GeometryGraph layerGraph = graphPerLayer[srcFsmNode.LayerIndex];

                int width = (connection.Transitions.Any() && connection.Transitions[0].ConditionOperatorGroups.Any()) ? 150 : 50;
                Edge edge = new(layerGraph.FindNodeByUserData(connection.Source), layerGraph.FindNodeByUserData(connection.Target), width, 30, 20);
                layerGraph.Edges.Add(edge);
            }

            // At this point we have graphs for each layer. These layers aren't connected yet though.
            // Compute their layouts.
            foreach (var layerGraph in graphPerLayer)
            {
                var layerLayout = new LayeredLayout(layerGraph.Value, new SugiyamaLayoutSettings
                {
                    Transformation = PlaneTransformation.Rotation(Math.PI / 2), // Make LR (Left-To-Right)
                    NodeSeparation = 80,
                    EdgeRoutingSettings = { EdgeRoutingMode = EdgeRoutingMode.StraightLine },
                });

                layerLayout.Run();
            }

            // Create the master graph. That one will hold all root nodes & layers.
            var mainGraph = new GeometryGraph();

            // Add the root nodes.
            Dictionary<int /* layerIndex*/, FsmGroupNodeViewModel> layerToGroupNode = [];
            const int GroupEdgePadding = 15;
            const int VerticalPadding = 40; // 30 (group header height more or less)

            var editorVm = (FsmEditorViewModel)Editor.DataContext!;

            if (graphPerLayer.Count > 0)
            {
                foreach (var layerGraph in graphPerLayer)
                {
                    // For layers, we need to create a global group node
                    // Note that the root layer won't actually have one

                    double minX = float.MaxValue;
                    double minY = float.MaxValue;
                    double maxX = float.MinValue;
                    double maxY = float.MinValue;

                    if (editorVm.LayerGroups.TryGetValue(layerGraph.Key, out FsmGroupNodeViewModel? groupVm))
                    {
                        foreach (var node in layerGraph.Value.Nodes)
                        {
                            Microsoft.Msagl.Core.Geometry.Point nodeStart = node.BoundingBox.LeftBottom;
                            Microsoft.Msagl.Core.Geometry.Point nodeEnd = node.BoundingBox.RightTop;

                            if (nodeStart.X < minX)
                                minX = nodeStart.X;

                            if (nodeStart.Y < minY)
                                minY = nodeStart.Y;

                            if (nodeEnd.X > maxX)
                                maxX = nodeEnd.X;

                            if (nodeEnd.Y > maxY)
                                maxY = nodeEnd.Y;

                            groupVm.Location = new Point(minX, minY);
                        }

                        // For width height we use the graph's rather than compute it.
                        // The graph's bbox may be larger than the actual max coordinates (max - min) of the nodes.

                        // The padding is so that the node aren't literally colliding with the group's edges.
                        groupVm.Size = new Size(layerGraph.Value.Width + (GroupEdgePadding * 2),
                            layerGraph.Value.Height + (VerticalPadding + GroupEdgePadding)); // Top (header) padding + bottom.

                        var layerNode = new Microsoft.Msagl.Core.Layout.Node(CreateCurve(groupVm.Size.Width, groupVm.Size.Height), groupVm);
                        mainGraph.Nodes.Add(layerNode);

                        layerToGroupNode.Add(layerGraph.Key, groupVm);
                    }
                }
            }

            // Our main graph has all the root nodes as well as each layer now.
            // Connect everything.
            foreach (FsmConnectionViewModel connection in Editor.Connections)
            {
                FsmNodeViewModel srcFsmNode = (FsmNodeViewModel)connection.Source;
                FsmNodeViewModel dstFsmNode = (FsmNodeViewModel)connection.Target;
                if (srcFsmNode.LayerIndex != dstFsmNode.LayerIndex) // Layer to layer connection.
                {
                    var sourceNode = mainGraph.FindNodeByUserData(layerToGroupNode[srcFsmNode.LayerIndex]);
                    var targetNode = mainGraph.FindNodeByUserData(layerToGroupNode[dstFsmNode.LayerIndex]);

                    Edge edge = new(sourceNode, targetNode, 30, 30, 20);
                    mainGraph.Edges.Add(edge);
                }
                // else {} - We don't care for regular transitions happening in sub-layers.
            }

            var layout = new LayeredLayout(mainGraph, new SugiyamaLayoutSettings
            {
                Transformation = PlaneTransformation.Rotation(Math.PI / 2), // Make LR (Left-To-Right)
                NodeSeparation = 80,
                EdgeRoutingSettings = { EdgeRoutingMode = EdgeRoutingMode.StraightLine },
            });

            layout.Run();

            foreach (var graphNode in mainGraph.Nodes)
            {
                NodeViewModelBase nvm = (NodeViewModelBase)graphNode.UserData;

                // Reminder: Nodify's node Location = TopLeft in regards to its graph
                // So use MSAGL's BoundaryBox.LeftTop for most operations

                nvm.Location = new Point(graphNode.BoundingBox.Left - mainGraph.BoundingBox.Center.X,
                                         graphNode.BoundingBox.Bottom - mainGraph.BoundingBox.Center.Y);

                if (nvm is FsmGroupNodeViewModel groupNode)
                {
                    // Layer Graph nodes -> Main Graph
                    GeometryGraph layerGraph = graphPerLayer[groupNode.LayerIndex];
                    foreach (var layerNode in graphPerLayer[groupNode.LayerIndex].Nodes)
                    {
                        NodeViewModel subNvm = (NodeViewModel)layerNode.UserData;
                        double subNodeX = Math.Abs(layerNode.BoundingBox.LeftTop.X - layerGraph.BoundingBox.LeftTop.X);
                        double subNodeY = Math.Abs(layerNode.BoundingBox.LeftTop.Y - layerGraph.BoundingBox.LeftTop.Y);

                        // GroupPadding = left edge padding.
                        subNvm.Location = new Point(GroupEdgePadding + groupNode.Location.X + subNodeX,
                            VerticalPadding + groupNode.Location.Y + subNodeY);
                    }
                }
            }
        }
    }

    private GeometryGraph CreateGraphForLayer(IGrouping<int, NodeViewModel> layer)
    {
        var graph = new GeometryGraph();

        foreach (NodeViewModel groupNode in layer)
        {
            Control? control = Editor.ContainerFromItem(groupNode);

            double width = 100; double height = 100;
            if (control is not null)
            {
                width = control.DesiredSize.Width;
                height = control.DesiredSize.Height;
            }

            var node = new Microsoft.Msagl.Core.Layout.Node(CreateCurve(width, height), groupNode);
            graph.Nodes.Add(node);
        }

        return graph;
    }

    public static ICurve CreateCurve(double w, double h)
    {
        return CurveFactory.CreateRectangle(w, h, new Microsoft.Msagl.Core.Geometry.Point(w, h));
    }

    private void FsmNodeView_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Pointer.Type == PointerType.Mouse)
        {
            var properties = e.GetCurrentPoint(this).Properties;
            if (!properties.IsRightButtonPressed)
                return;

            FsmNodeView nodeView = (FsmNodeView)sender!;
            var nodeVM = (FsmNodeViewModel)nodeView.DataContext!;
            var editor = (FsmEditorViewModel)DataContext!;

            ObservableCollection<MenuItemViewModel> items;
            if (editor.SelectedNodes.Count <= 1)
                items = editor.CreateContextMenuForNode(nodeVM);
            else
                items = editor.CreateContextMenuForMultipleNodes(editor.SelectedNodes);

            var flyout = new ExtendableMenuFlyout
            {
                ItemsSource = items
            };
            flyout.ShowAt(nodeView, showAtPointer: true);
        }
    }

    private void MenuFlyout_Opening(object? sender, EventArgs e)
    {
        var vm = this.DataContext as FsmEditorViewModel;
        bool isInLayer = false;

        var layerBbox = new Rect(vm!.MouseLocation.X, vm.MouseLocation.Y, 150, 30); // Size of default group node as per GroupingNode.yaml

        foreach (var group in vm.LayerGroups.Values)
        {
            if (group.LayerIndex == 0)
                continue;

            if (group.BoundaryBox.Intersects(layerBbox))
            {
                isInLayer = true;
                break;
            }
        }

        vm.AddLayerMenuItem.Enabled = !isInLayer;
    }

    private void GroupingNode_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (e.Pointer.Type == PointerType.Mouse)
        {
            var properties = e.GetCurrentPoint(this).Properties;
            if (!properties.IsRightButtonPressed)
                return;

            var groupingNode = (GroupingNode)sender!;
            var layerNode = (FsmGroupNodeViewModel)groupingNode.DataContext!;

            var editorVm = (FsmEditorViewModel)DataContext!;

            ObservableCollection<MenuItemViewModel> items = editorVm.CreateContextMenuForLayer(layerNode);

            var flyout = new ExtendableMenuFlyout
            {
                ItemsSource = items,
            };
            flyout.ShowAt(groupingNode, showAtPointer: true);

            e.Handled = true;
        }
    }

    // Commented out - tried to use the clustering feature from msagl
    // Ideally should be using FastIncrementalLayout (which has better cluster support)
    // (relevant msagl sample: FastIncrementalLayoutWithGdi)

    #region Unused
    /// <summary>
    /// Fired when nodes are loaded.
    /// Any automatic node layouting happens here.
    /// </summary>
    ///
    private void PerformAutomaticNodeLayoutingClustered()
    {
        var editorVm = (FsmEditorViewModel)Editor.DataContext!;
        if (Editor.Items.Any() && Editor.Tag as bool? != true)
        {
            // Nodes are now loaded. Proceed to create an automated layout
            // Ensure to update everything
            Editor.UpdateLayout();

            // Each layer will go into a different graph.
            // All root nodes will be grouped aswell for display purposes. They just won't actually be inserted into a group.
            ItemCollection items = Editor.Items;
            var groups = items.Where(e => e is FsmNodeViewModel)
                .Cast<FsmNodeViewModel>()
                .GroupBy(e => e.LayerIndex);

            Dictionary<int /* layerIndex */, Cluster> graphPerLayer = [];


            // Create the master graph. That one will hold all root nodes & layers.
            var mainGraph = new GeometryGraph();

            foreach (var layer in groups)
            {
                var cluster = new Cluster();
                cluster.UserData = editorVm.LayerGroups[layer.Key];
                foreach (NodeViewModel groupNode in layer)
                {
                    Control? control = Editor.ContainerFromItem(groupNode);

                    double width = 100; double height = 100;
                    if (control is not null)
                    {
                        width = control.DesiredSize.Width;
                        height = control.DesiredSize.Height;
                    }

                    var node = new Microsoft.Msagl.Core.Layout.Node(CreateCurve(width, height), groupNode);
                    mainGraph.Nodes.Add(node);
                    cluster.AddChild(node);
                }

                if (layer.Key != 0)
                    mainGraph.RootCluster.AddChild(cluster);

                graphPerLayer.Add(layer.Key, cluster);
            }

            foreach (FsmConnectionViewModel connection in Editor.Connections)
            {
                FsmNodeViewModel srcFsmNode = (FsmNodeViewModel)connection.Source;
                FsmNodeViewModel dstFsmNode = (FsmNodeViewModel)connection.Target;

                if (srcFsmNode.LayerIndex != dstFsmNode.LayerIndex)
                    continue;

                Cluster layerCluster = graphPerLayer[srcFsmNode.LayerIndex];

                int width = (connection.Transitions.Any() && connection.Transitions[0].ConditionOperatorGroups.Any()) ? 150 : 50;
                Edge edge = new(layerCluster.Nodes.FirstOrDefault(e => e.UserData == connection.Source), layerCluster.Nodes.FirstOrDefault(e => e.UserData == connection.Target), width, 30, 20);
                mainGraph.Edges.Add(edge);
            }


            // Add the root nodes.
            Dictionary<int /* layerIndex*/, FsmGroupNodeViewModel> layerToGroupNode = [];
            const int GroupEdgePadding = 15;
            const int VerticalPadding = 40; // 30 (group header height more or less)

            // Our main graph has all the root nodes as well as each layer now.
            // Connect everything.
            foreach (FsmConnectionViewModel connection in Editor.Connections)
            {
                FsmNodeViewModel srcFsmNode = (FsmNodeViewModel)connection.Source;
                FsmNodeViewModel dstFsmNode = (FsmNodeViewModel)connection.Target;

                if (srcFsmNode.LayerIndex != dstFsmNode.LayerIndex) // Layer to layer connection.
                {
                    var sourceNode = graphPerLayer[srcFsmNode.LayerIndex].Nodes.FirstOrDefault(e => e.UserData == connection.Source);
                    var targetNode = graphPerLayer[srcFsmNode.LayerIndex].Nodes.FirstOrDefault(e => e.UserData == connection.Target);

                    Edge edge = new(sourceNode, targetNode, 30, 30, 20);
                    mainGraph.Edges.Add(edge);

                    graphPerLayer[srcFsmNode.LayerIndex].AddOutEdge(new Edge(graphPerLayer[srcFsmNode.LayerIndex],
                        graphPerLayer[dstFsmNode.LayerIndex]));
                    graphPerLayer[dstFsmNode.LayerIndex].AddInEdge(new Edge(graphPerLayer[dstFsmNode.LayerIndex],
                        graphPerLayer[srcFsmNode.LayerIndex]));
                }
                // else {} - We don't care for regular transitions happening in sub-layers.
            }

            var settings = new SugiyamaLayoutSettings
            {
                Transformation = PlaneTransformation.Rotation(Math.PI / 2), // Make LR (Left-To-Right)
                NodeSeparation = 80,

                EdgeRoutingSettings = { EdgeRoutingMode = EdgeRoutingMode.StraightLine },
            };

            foreach (var cluster in mainGraph.RootCluster.Clusters)
            {
                settings.ClusterSettings.Add(cluster, settings.Clone());
            }

            var layout = new LayeredLayout(mainGraph, settings);
            layout.Run();

            foreach (var graphNode in mainGraph.Nodes)
            {
                NodeViewModelBase nvm = (NodeViewModelBase)graphNode.UserData;

                // Reminder: Nodify's node Location = TopLeft in regards to its graph
                // So use MSAGL's BoundaryBox.LeftTop for most operations

                nvm.Location = new Point(graphNode.BoundingBox.Left - mainGraph.BoundingBox.Center.X,
                                         graphNode.BoundingBox.Bottom - mainGraph.BoundingBox.Center.Y);
            }

            foreach (var cluster in mainGraph.RootCluster.Clusters)
            {
                foreach (var graphNode in cluster.Nodes)
                {
                    NodeViewModelBase nvm = (NodeViewModelBase)graphNode.UserData;
                    nvm.Location = new Point(graphNode.BoundingBox.Left - mainGraph.BoundingBox.Center.X,
                                            graphNode.BoundingBox.Bottom - mainGraph.BoundingBox.Center.Y);
                }
            }
        }

        var firstNode = editorVm.LayerGroups.Count > 0 ? editorVm.LayerGroups.FirstOrDefault().Value : null;
        if (firstNode is not null)
        {
            Editor.BringIntoView(firstNode.Location, animated: false);
        }

        Editor.Tag = true;
    }
    #endregion
}