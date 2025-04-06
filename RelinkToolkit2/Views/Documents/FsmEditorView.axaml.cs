using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using GBFRDataTools.FSM.Components;

using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;

using Microsoft.Msagl.Layout.Layered;

using Nodify;

using RelinkToolkit2.Controls;
using RelinkToolkit2.Messages;
using RelinkToolkit2.Messages.Fsm;
using RelinkToolkit2.ViewModels;
using RelinkToolkit2.ViewModels.Documents;
using RelinkToolkit2.ViewModels.Fsm;
using RelinkToolkit2.ViewModels.Menu;
using RelinkToolkit2.ViewModels.Search;
using RelinkToolkit2.Views.Fsm;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace RelinkToolkit2.Views.Documents;

public partial class FsmEditorView : UserControl
{
    public FsmEditorView()
    {
        InitializeComponent();
    }

    private void RegisterMessages()
    {
        WeakReferenceMessenger.Default.Register<BringFsmNodeIntoViewRequest>(this, (recipient, message) =>
        {
            if (DataContext is not FsmEditorViewModel editorVm)
                return;

            if (!editorVm.Nodes.Contains(message.Node))
                return;

            Editor.BringIntoView(message.Node.Center, animated: message.Animated);
            Editor.ZoomAtPosition(1.0f, message.Node.Center);
            message.Reply(true);
        });

        WeakReferenceMessenger.Default.Register<GetNodeControlRequest>(this, (recipient, message) =>
        {
            Control? control = Editor.ContainerFromItem(message.Node);
            if (control is null)
                return;

            message.Reply(control);
        });

        WeakReferenceMessenger.Default.Register<DismissSearchComponentMenuRequest>(this, (recipient, message) =>
        {
            _searchFlyout?.Hide();
            message.Reply(true);
        });

        WeakReferenceMessenger.Default.Register<FsmComponentContextMenuRequest>(this, (recipient, message) =>
        {
            ObservableCollection<MenuItemViewModel> items = CreateNodeContextMenu(message.NodeView, message.Component.Parent, message.Component);

            var flyout = new MenuFlyout();
            flyout.ItemsSource = items;
            flyout.ShowAt(message.NodeView, showAtPointer: true);

            message.Reply(true);
        });
    }

    private void NodifyEditor_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var test = BindingOperations.GetBindingExpressionBase(Editor, NodifyEditor.PendingConnectionProperty);

        RegisterMessages();
        PerformAutomaticNodeLayouting();
    }

    private void NodifyEditor_Unloaded_1(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        UnregisterMessages();
    }

    private void UnregisterMessages()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    /// <summary>
    /// Fired when nodes are loaded.
    /// Any automatic node layouting happens here.
    /// </summary>
    private void PerformAutomaticNodeLayouting()
    {
        if (Editor.Items.Any() && Editor.Tag as bool? != true)
        {
            // Nodes are now loaded. Proceed to create an automated layout
            // Ensure to update everything
            Editor.UpdateLayout();

            // Each layer will go into a different graph.
            // All root nodes will be grouped aswell for display purposes. They just won't actually be inserted into a group.
            ItemCollection items = Editor.Items;
            var groups = items.Where(e => e is NodeViewModel)
                .Cast<NodeViewModel>()
                .GroupBy(e => e.LayerIndex);

            Dictionary<int /* layerIndex */, GeometryGraph> graphPerLayer = [];
            foreach (var layer in groups)
            {
                GeometryGraph layerGraph = CreateGraphForLayer(layer);
                graphPerLayer.Add(layer.Key, layerGraph);
            }

            // Link nodes together. We won't link layers together yet as we need to compute how big they are.
            foreach (GraphConnectionViewModel connection in Editor.Connections)
            {
                if (connection.Source.LayerIndex != connection.Target.LayerIndex)
                    continue;

                GeometryGraph layerGraph = graphPerLayer[connection.Source.LayerIndex];

                int width = (connection.Transitions.Any() && connection.Transitions[0].ConditionComponents.Any()) ? 150 : 50;
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
            Dictionary<int /* layerIndex*/, GroupNodeViewModel> layerToGroupNode = [];
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

                    GroupNodeViewModel groupVm = editorVm.LayerGroups[layerGraph.Key];
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

            // Our main graph has all the root nodes as well as each layer now.
            // Connect everything.
            foreach (GraphConnectionViewModel connection in Editor.Connections)
            {
                if (connection.Source.LayerIndex != connection.Target.LayerIndex) // Layer to layer connection.
                {
                    var sourceNode = mainGraph.FindNodeByUserData(layerToGroupNode[connection.Source.LayerIndex]);
                    var targetNode = mainGraph.FindNodeByUserData(layerToGroupNode[connection.Target.LayerIndex]);

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

                if (nvm is GroupNodeViewModel groupNode)
                {
                    if (groupNode.LayerIndex != 0)
                    {
                        editorVm.Nodes.Add(groupNode);
                    }

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

            if (graphPerLayer.Count != 0 && graphPerLayer.TryGetValue(0, out GeometryGraph? rootLayer) && rootLayer.Nodes.Count > 0)
            {
                var firstNode = (NodeViewModelBase)rootLayer.Nodes[0].UserData;
                Editor.BringIntoView(firstNode.Location, animated: false);
            }

            Editor.Tag = true;
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
            var nodeVM = (NodeViewModel)nodeView.DataContext!;

            ObservableCollection<MenuItemViewModel> items = CreateNodeContextMenu(nodeView, nodeVM);

            var flyout = new MenuFlyout();
            flyout.ItemsSource = items;
            flyout.ShowAt(nodeView, showAtPointer: true);
        }
    }

    private ObservableCollection<MenuItemViewModel> CreateNodeContextMenu(FsmNodeView nodeView, NodeViewModel nodeVM, NodeComponentViewModel? componentVM = null)
    {
        bool canAddComponent = !nodeVM.IsEndNode && // End nodes don't physically exist therefore they cannot have components
            (string.IsNullOrEmpty(nodeVM.FsmName) && string.IsNullOrEmpty(nodeVM.FsmFolderName)) && // Nodes that call fsm files don't seem to have components
            !nodeVM.IsLayerRootNode; // Root nodes cannot have components it seems

        bool canDeleteNode = !nodeVM.IsLayerRootNode;

        ObservableCollection<MenuItemViewModel> items =
        [
            new MenuItemViewModel()
                {
                    Header = nodeVM.Title,
                    FontWeight = FontWeight.Bold,
                    IconKind = "Material.CogBox",
                    Enabled = true,
                    IsHitTestVisible = false, // Not greyed out but not clickable
                },
                MenuItemViewModel.Separator,
        ];

        items.Add(new MenuItemViewModel()
        {
            Header = $"Add Component",
            IconKind = "Material.CardPlus",
            Enabled = canAddComponent,
            Command = new RelayCommand<FsmNodeView>(AddComponent),
            Parameter = nodeView,
        });

        items.Add(new MenuItemViewModel()
        {
            Header = $"Create Self Transition",
            IconKind = "Material.ShapeCirclePlus",
            Enabled = !nodeVM.HasSelfTransition && !nodeVM.IsEndNode,
            Command = new RelayCommand<NodeViewModel>(CreateSelfTransition),
            Parameter = nodeVM,
        });

        items.Add(new MenuItemViewModel()
        {
            Header = $"Edit Name",
            IconKind = "Material.Pencil",
            Enabled = true,
            Command = new RelayCommand<NodeViewModel>(EditNodeName),
            Parameter = nodeVM,
        });

        items.Add(MenuItemViewModel.Separator);
        items.Add(new MenuItemViewModel()
        {
            Header = $"Copy Guid ({nodeVM.Guid})",
            Enabled = true,
            IconKind = "Material.ContentCopy",
            Command = new RelayCommand<FsmNodeView>(CopyGuid),
            Parameter = nodeView,
        });
        items.Add(MenuItemViewModel.Separator);

        if (nodeVM.Transitions.Count > 0)
        {
            var linkedNodesMenu = new MenuItemViewModel()
            {
                Header = "Transitions...",
                IconKind = "Material.RayStartArrow",
            };

            foreach (var linkedNode in nodeVM.Transitions)
            {
                if (linkedNode.Source == nodeVM)
                {
                    linkedNodesMenu.MenuItems.Add(new MenuItemViewModel()
                    {
                        Header = linkedNode.Target.Title,
                        Enabled = true,
                        MenuItems =
                        [
                            new MenuItemViewModel()
                            {
                                Header = "Edit Connection...",
                                IconKind = "Material.TransitConnectionHorizontal",
                                Enabled = true,
                                Command = new RelayCommand<GraphConnectionViewModel>(ConnectionSelected),
                                Parameter = linkedNode.ParentConnection,
                            },
                            new MenuItemViewModel()
                            {
                                Header = "Bring Node into View",
                                IconKind = "Material.MagnifyExpand",
                                Enabled = true,
                                Command = new RelayCommand<NodeViewModel>(BringNodeIntoView),
                                Parameter = linkedNode.Target,
                            },
                        ],
                        Parameter = linkedNode.ParentConnection,
                    });
                }
            }
            linkedNodesMenu.Enabled = linkedNodesMenu.MenuItems.Count != 0;
            items.Add(linkedNodesMenu);
            items.Add(MenuItemViewModel.Separator);
        }

        if (componentVM is not null)
        {
            items.Add(new MenuItemViewModel()
            {
                Header = $"Delete Component ({componentVM.Name})",
                IconKind = "Material.Delete",
                Enabled = true,
                Command = new RelayCommand<NodeComponentViewModel>(DeleteComponent),
                Parameter = componentVM,
            });
        }

        items.Add(new MenuItemViewModel()
        {
            Header = $"Delete Node",
            IconKind = "Material.Delete",
            Enabled = canDeleteNode,
            Command = new RelayCommand<NodeViewModel>(DeleteNode),
            Parameter = nodeVM,
        });

        return items;
    }

    private void DeleteComponent(NodeComponentViewModel? componentVM)
    {
        componentVM!.Parent.DeleteComponent(componentVM);
    }

    private void EditNodeName(NodeViewModel? nodeVM)
    {
        nodeVM!.IsRenaming = true;
    }

    private void ConnectionSelected(GraphConnectionViewModel? graphConnection)
    {
        WeakReferenceMessenger.Default.Send(new EditConnectionRequest(graphConnection));
    }

    private void BringNodeIntoView(NodeViewModel? nodeVM)
    {
        WeakReferenceMessenger.Default.Send(new BringFsmNodeIntoViewRequest(nodeVM!));
    }

    private Flyout? _searchFlyout;
    private void AddComponent(FsmNodeView? nodeView)
    {
        _searchFlyout?.Hide();

        if (_searchFlyout is null)
        {
            _searchFlyout ??= new Flyout() { ShowMode = FlyoutShowMode.Transient };
            _searchFlyout.VerticalOffset = 4;
            _searchFlyout.OverlayDismissEventPassThrough = true;
        }

        BuildSearch(nodeView!); // TODO: Cache?
        _searchFlyout.ShowAt(nodeView!, showAtPointer: false);
    }

    private static IEnumerable<Type> _actionComponentTypes = Assembly.GetAssembly(typeof(BehaviorTreeComponent))!.GetTypes()
            .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(ActionComponent)));

    private void BuildSearch(FsmNodeView nodeView)
    {
        var content = new ComponentSearchView();
        _searchFlyout!.Content = content;

        var searchVM = new ComponentSearchViewModel();
        content.DataContext = searchVM;
        searchVM.Context = nodeView.DataContext as NodeViewModel;
        
        string baseNamespace = typeof(ActionComponent).Namespace!;
        ComponentSearchPageViewModel mainPage = new ComponentSearchPageViewModel();
        foreach (Type compType in _actionComponentTypes)
        {
            if (compType.Namespace is null)
                continue;

            if (compType.Namespace.StartsWith("GBFRDataTools.FSM.Components.Actions"))
            {
                string desc = compType.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty;

                if (compType.Namespace == "GBFRDataTools.FSM.Components.Actions")
                {
                    mainPage.AddComponent(compType.Name, compType, desc, OnAddComponent);
                }
                else
                {
                    string subNamespace = compType.Namespace["GBFRDataTools.FSM.Components.Actions".Length..];
                    if (!string.IsNullOrEmpty(subNamespace))
                    {
                        string[] subSplit = subNamespace.Substring(1).Split('.');
                        var currentPage = mainPage;

                        string pageName = "";
                        for (int i = 0; i < subSplit.Length; i++)
                        {
                            string? folder = subSplit[i];
                            pageName += folder;

                            if (!currentPage.TryGetByName(folder, out ComponentSearchItemViewModel component))
                            {
                                component = new ComponentSearchItemViewModel() { Name = folder };
                                currentPage.AddComponent(component);

                                ComponentSearchPageViewModel subPage = new()
                                {
                                    Title = pageName,
                                };

                                component.SetPage(subPage);
                                currentPage = subPage;
                            }
                            else
                            {
                                currentPage = ((ComponentSearchPageViewModel)component.Data!);
                            }

                            if (i == subSplit.Length - 1)
                                currentPage.AddComponent(compType.Name, compType, desc, OnAddComponent);
                            else
                                pageName += " > ";
                        }
                    }
                }
            }
        }

        searchVM.AddPage(mainPage);
        searchVM.SortAndBuildSearch();
    }

    private void OnAddComponent(ComponentSearchViewModel searchVM, ComponentSearchItemViewModel item)
    {
        if (item.Data is not Type type)
            return;

        if (searchVM.Context is not NodeViewModel nvm)
            return;

        var component = Activator.CreateInstance(type);
        if (component is not BehaviorTreeComponent bt)
            return;

        var editor = (FsmEditorViewModel)this.DataContext!;
        bt.ParentGuid = nvm.Guid;
        bt.Guid = editor.GetNewGuid();
        editor.RegisterFsmElementGuid(bt.Guid, bt);

        var componentVM = new NodeComponentViewModel(nvm) { Name = type.Name, Component = bt };
        nvm.Components.Add(componentVM);

        // Make sure to select it.
        WeakReferenceMessenger.Default.Send(new FsmComponentSelectedMessage(bt));
    }

    private void CreateSelfTransition(NodeViewModel? nodeViewModel)
    {
        nodeViewModel!.HasSelfTransition = true;

        var connection = new GraphConnectionViewModel() { Source = nodeViewModel, Target = nodeViewModel };
        var transition = new TransitionViewModel(connection)
        {
            Source = nodeViewModel,
            Target = nodeViewModel
        };
        connection.Transitions.Add(transition);
        nodeViewModel.Transitions.Add(transition);

        WeakReferenceMessenger.Default.Send(new EditConnectionRequest(connection));
    }

    private void DeleteNode(NodeViewModel? node)
    {
        var editorVm = (FsmEditorViewModel)Editor.DataContext!;
        editorVm.RemoveNode(node!);
    }

    private async void CopyGuid(FsmNodeView? nodeView)
    {
        if (nodeView is null)
            return;

        var clipboard = TopLevel.GetTopLevel(nodeView)?.Clipboard;
        if (clipboard is not null)
        {
            var nodeVM = (NodeViewModel)nodeView.DataContext!;

            var dataObject = new DataObject();
            dataObject.Set(DataFormats.Text, nodeVM.Guid.ToString());
            await clipboard.SetDataObjectAsync(dataObject);
        }
    }
}