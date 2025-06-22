using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;

using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using DynamicData;

using GBFRDataTools.FSM.Components;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;

using Microsoft.Msagl.Layout.Layered;

using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

using Nodify;

using RelinkToolkit2.Messages.Fsm;
using RelinkToolkit2.Services;
using RelinkToolkit2.ViewModels.Documents;
using RelinkToolkit2.ViewModels.Documents.GraphEditor;
using RelinkToolkit2.ViewModels.Documents.GraphEditor.Nodes;
using RelinkToolkit2.ViewModels.Menu;
using RelinkToolkit2.ViewModels.Search;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RelinkToolkit2.Views.Documents.GraphEditor;

public partial class BTEditorView : UserControl
{
    public BTEditorView()
    {
        InitializeComponent();
    }

    private void RegisterMessages()
    {
        WeakReferenceMessenger.Default.Register<BringFsmNodeIntoViewRequest>(this, (recipient, message) =>
        {
            if (DataContext is not BTEditorViewModel editorVm)
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
            ObservableCollection<MenuItemViewModel> items = CreateNodeContextMenu((BTNodeView)message.NodeView, (BTNodeViewModel)message.Component.Parent, message.Component);

            var flyout = new MenuFlyout();
            flyout.ItemsSource = items;
            flyout.ShowAt(message.NodeView, showAtPointer: true);

            message.Reply(true);
        });
    }

    private void NodifyEditor_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        RegisterMessages();
        PerformAutomaticNodeLayouting();

        // Highlight first node if needed
        var editorVM = Editor.DataContext as BTEditorViewModel;
        var firstNode = editorVM.Nodes.Count > 0 ? editorVM.Nodes[0] : null;
        if (Editor.Tag is null && firstNode is not null)
        {
            Editor.BringIntoView(firstNode.Center, animated: false);
        }

        Editor.Tag = true;
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
        var editorVM = Editor.DataContext as BTEditorViewModel;
        if (Editor.Items.Any() && !editorVM.IsLayouted)
        {
            // Nodes are now loaded. Proceed to create an automated layout
            // Ensure to update everything
            Editor.UpdateLayout();

            var geometryGraph = new GeometryGraph();
            foreach (BTNodeViewModel node in editorVM.Nodes)
            {
                AddGraphNode(geometryGraph, node);
            }

            foreach (GraphConnectionViewModel connection in editorVM.Connections)
            {
                geometryGraph.Edges.Add(new Edge(geometryGraph.FindNodeByUserData(connection.Source), geometryGraph.FindNodeByUserData(connection.Target), 30, 30, 1));
            }

            var layerLayout = new LayeredLayout(geometryGraph, new SugiyamaLayoutSettings
            {
                Transformation = PlaneTransformation.Rotation(Math.PI),
                PackingMethod = PackingMethod.Columns,
                NodeSeparation = 100,
                EdgeRoutingSettings = { EdgeRoutingMode = EdgeRoutingMode.Rectilinear },
                MinNodeHeight = 100,
                MinNodeWidth = 100,
            });

            layerLayout.Run();

            foreach (var graphNode in geometryGraph.Nodes)
            {
                BTNodeViewModel nvm = (BTNodeViewModel)graphNode.UserData;
                // Reminder: Nodify's node Location = TopLeft in regards to its graph
                // So use MSAGL's BoundaryBox.LeftTop for most operations

                nvm.Location = new Point(graphNode.BoundingBox.Left, graphNode.BoundingBox.Top);
            }
        }
    }

    private void AddGraphNode(GeometryGraph graph, BTNodeViewModel btNode)
    {
        Control? control = Editor.ContainerFromItem(btNode);

        double width = 100; double height = 100;
        if (control is not null)
        {
            width = control.DesiredSize.Width;
            height = control.DesiredSize.Height;
        }

        var node = new Microsoft.Msagl.Core.Layout.Node(CreateCurve(width, height), btNode);
        graph.Nodes.Add(node);
    }

    public static ICurve CreateCurve(double w, double h)
    {
        return CurveFactory.CreateRectangle(w, h, new Microsoft.Msagl.Core.Geometry.Point(0, 0));
    }

    private void FsmNodeView_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Pointer.Type == PointerType.Mouse)
        {
            var properties = e.GetCurrentPoint(this).Properties;
            if (!properties.IsRightButtonPressed)
                return;

            BTNodeView nodeView = (BTNodeView)sender!;
            var nodeVM = (BTNodeViewModel)nodeView.DataContext!;

            ObservableCollection<MenuItemViewModel> items = CreateNodeContextMenu(nodeView, nodeVM);

            var flyout = new MenuFlyout();
            flyout.ItemsSource = items;
            flyout.ShowAt(nodeView, showAtPointer: true);
        }
    }

    private ObservableCollection<MenuItemViewModel> CreateNodeContextMenu(BTNodeView nodeView, BTNodeViewModel nodeVM, NodeComponentViewModel? componentVM = null)
    {
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
            IconKind = "Material.PuzzlePlus",
            Enabled = true,
            Command = new RelayCommand<BTNodeView>(NodeContextMenu_AddComponent),
            Parameter = nodeView,
        });

        items.Add(new MenuItemViewModel()
        {
            Header = $"Edit Name",
            IconKind = "Material.Pencil",
            Enabled = true,
            Command = new RelayCommand<BTNodeViewModel>(NodeContextMenu_EditNodeName),
            Parameter = nodeVM,
        });

        items.Add(new MenuItemViewModel()
        {
            Header = $"Copy Guid ({nodeVM.Guid})",
            Enabled = true,
            IconKind = "Material.ContentCopy",
            Command = new RelayCommand<BTNodeView>(NodeContextMenu_CopyGuid),
            Parameter = nodeView,
        });
        items.Add(MenuItemViewModel.Separator);

        if (componentVM is not null)
        {
            items.Add(new MenuItemViewModel()
            {
                Header = $"Delete Component ({componentVM.Name})",
                IconKind = "Material.Delete",
                Enabled = true,
                Command = new RelayCommand<NodeComponentViewModel>(NodeContextMenu_DeleteComponent),
                Parameter = componentVM,
            });
        }

        items.Add(new MenuItemViewModel()
        {
            Header = $"Delete Node",
            IconKind = "Material.Delete",
            Enabled = false /*canDeleteNode*/,
            Command = new RelayCommand<BTNodeViewModel>(NodeContextMenu_DeleteNode),
            Parameter = nodeVM,
        });

        return items;
    }

    private void NodeContextMenu_DeleteComponent(NodeComponentViewModel? componentVM)
    {
        ((BTNodeViewModel)componentVM!.Parent).DeleteComponent(componentVM);
    }

    private void NodeContextMenu_EditNodeName(BTNodeViewModel? nodeVM)
    {
        nodeVM!.IsRenaming = true;
    }


    private void NodeContextMenu_DeleteConnection(GraphConnectionViewModel graphConnection)
    {
        var editor = (BTEditorViewModel)DataContext!;
        editor.RemoveConnection(graphConnection);
    }

    private void NodeContextMenu_BringNodeIntoView(NodeViewModel? nodeVM)
    {
        WeakReferenceMessenger.Default.Send(new BringFsmNodeIntoViewRequest(nodeVM!));
    }

    private Flyout? _searchFlyout;
    private void NodeContextMenu_AddComponent(BTNodeView? nodeView)
    {
        _searchFlyout?.Hide();

        if (_searchFlyout is null)
        {
            _searchFlyout ??= new Flyout() { ShowMode = FlyoutShowMode.Transient };
            _searchFlyout.VerticalOffset = 4;
            _searchFlyout.OverlayDismissEventPassThrough = true;
        }

        BuildActionSearchMenu(nodeView!); // TODO: Cache?
        _searchFlyout.ShowAt(nodeView!, showAtPointer: false);
    }

    private static IEnumerable<Type> _actionComponentTypes = Assembly.GetAssembly(typeof(BehaviorTreeComponent))!.GetTypes()
            .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(ActionComponent)));

    private void BuildActionSearchMenu(BTNodeView nodeView)
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
                                currentPage = (ComponentSearchPageViewModel)component.Data!;
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

        if (searchVM.Context is not BTNodeViewModel nodeViewModel)
            return;

        var component = Activator.CreateInstance(type);
        if (component is not BehaviorTreeComponent btComponent)
            return;

        var editor = (BTEditorViewModel)DataContext!;
        btComponent.ParentGuid = nodeViewModel.Guid;
        btComponent.Guid = editor.GetNewGuid();

        editor.RegisterBtElementGuid(btComponent.Guid, btComponent);

        nodeViewModel.AddComponent(btComponent);

        // Make sure to select it.
        WeakReferenceMessenger.Default.Send(new FsmComponentSelectedMessage(btComponent));
    }

    private void NodeContextMenu_DeleteNode(BTNodeViewModel? node)
    {
        var editorVm = (BTEditorViewModel)Editor.DataContext!;
        editorVm.RemoveNode(node!);
    }

    private async void NodeContextMenu_CopyGuid(BTNodeView? nodeView)
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