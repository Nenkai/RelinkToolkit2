using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;

using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

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
using RelinkToolkit2.ViewModels.Documents.GraphEditor.TransitionComponents;
using RelinkToolkit2.ViewModels.Menu;
using RelinkToolkit2.ViewModels.Search;
using RelinkToolkit2.Views.Documents;

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
            ObservableCollection<MenuItemViewModel> items = CreateNodeContextMenu((FsmNodeView)message.NodeView, (FsmNodeViewModel)message.Component.Parent, message.Component);

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
        var editorVM = Editor.DataContext as FsmEditorViewModel;
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
        var editorVM = Editor.DataContext as FsmEditorViewModel;
        if (Editor.Items.Any() && !editorVM.IsLayouted)
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

                    if (editorVm.LayerGroups.TryGetValue(layerGraph.Key, out FsmGroupNodeViewModel groupVm))
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

            ObservableCollection<MenuItemViewModel> items = CreateNodeContextMenu(nodeView, nodeVM);

            var flyout = new MenuFlyout();
            flyout.ItemsSource = items;
            flyout.ShowAt(nodeView, showAtPointer: true);
        }
    }

    private ObservableCollection<MenuItemViewModel> CreateNodeContextMenu(FsmNodeView nodeView, FsmNodeViewModel nodeVM, NodeComponentViewModel? componentVM = null)
    {
        bool canAddComponent = !nodeVM.IsEndNode && // End nodes don't physically exist therefore they cannot have components
            (string.IsNullOrEmpty(nodeVM.FsmName) && string.IsNullOrEmpty(nodeVM.FsmFolderName)) && // Nodes that call fsm files don't seem to have components
            !nodeVM.IsLayerRootNode; // Root nodes cannot have components it seems

        bool canBeEndNode = !nodeVM.IsEndNode && !nodeVM.IsLayerRootNode && nodeVM.Components.Count == 0 && nodeVM.Transitions.Count == 0;

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
            Enabled = canAddComponent,
            Command = new RelayCommand<FsmNodeView>(NodeContextMenu_AddComponent),
            Parameter = nodeView,
        });

        items.Add(new MenuItemViewModel()
        {
            Header = !nodeVM.HasSelfTransition ? "Create Self Transition" : $"Remove Self Transition",
            IconKind = "Material.ShapeCirclePlus",
            Enabled = (!nodeVM.HasSelfTransition && !nodeVM.IsEndNode) || nodeVM.HasSelfTransition,
            Command = new RelayCommand<FsmNodeViewModel>(NodeContextMenu_CreateSelfTransition!),
            Parameter = nodeVM,
        });

        items.Add(new MenuItemViewModel()
        {
            Header = $"Edit Name",
            IconKind = "Material.Pencil",
            Enabled = true,
            Command = new RelayCommand<FsmNodeViewModel>(NodeContextMenu_EditNodeName),
            Parameter = nodeVM,
        });

        bool hasBaseFsm = !string.IsNullOrEmpty(nodeVM.FsmName);
        items.Add(new MenuItemViewModel()
        {
            Header = !hasBaseFsm ? $"Link to Base FSM" : "Remove Base FSM Link",
            IconKind = !hasBaseFsm ? "Material.LinkVariantPlus" : "Material.LinkVariantRemove",
            Enabled = (!hasBaseFsm && !nodeVM.Components.Any() && !nodeVM.IsEndNode) || hasBaseFsm,
            Command = new AsyncRelayCommand<FsmNodeViewModel>(NodeContextMenu_LinkToBaseFSM!),
            Parameter = nodeVM,
        });

        items.Add(new MenuItemViewModel()
        {
            Header =  !nodeVM.IsEndNode ? "Set as End Node" : "Unset as End Node",
            IconKind = "Material.Octagon",
            Enabled = (!nodeVM.IsEndNode && canBeEndNode) || nodeVM.IsEndNode,
            Command = new RelayCommand<FsmNodeViewModel>(NodeContextMenu_SetAsEndNode),
            Parameter = nodeVM,
        });

        items.Add(new MenuItemViewModel()
        {
            Header = !nodeVM.IsBranch ? "Set As Branch" : $"Unset As Branch",
            IconKind = !nodeVM.IsBranch ? "Material.SourceBranchPlus" : "Material.SourceBranchMinus",
            Enabled = !nodeVM.IsEndNode,
            Command = new RelayCommand<FsmNodeViewModel>(NodeContextMenu_SetBranch!),
            Parameter = nodeVM,
        });

        items.Add(MenuItemViewModel.Separator);
        items.Add(new MenuItemViewModel()
        {
            Header = $"Copy Guid ({nodeVM.Guid})",
            Enabled = true,
            IconKind = "Material.ContentCopy",
            Command = new RelayCommand<FsmNodeView>(NodeContextMenu_CopyGuid),
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
                                Command = new RelayCommand<FsmConnectionViewModel>(NodeContextMenu_ConnectionSelected!),
                                Parameter = linkedNode.ParentConnection,
                            },
                            new MenuItemViewModel()
                            {
                                Header = "Delete Connection",
                                IconKind = "Material.Connection",
                                Enabled = true,
                                Command = new RelayCommand<FsmConnectionViewModel>(NodeContextMenu_DeleteConnection!),
                                Parameter = linkedNode.ParentConnection,
                            },
                            new MenuItemViewModel()
                            {
                                Header = "Bring into View",
                                IconKind = "Material.MagnifyExpand",
                                Enabled = true,
                                Command = new RelayCommand<NodeViewModel>(NodeContextMenu_BringNodeIntoView),
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
                Command = new RelayCommand<NodeComponentViewModel>(NodeContextMenu_DeleteComponent),
                Parameter = componentVM,
            });
        }

        items.Add(new MenuItemViewModel()
        {
            Header = $"Delete Node",
            IconKind = "Material.Delete",
            Enabled = canDeleteNode,
            Command = new RelayCommand<FsmNodeViewModel>(NodeContextMenu_DeleteNode),
            Parameter = nodeVM,
        });

        return items;
    }

    private void NodeContextMenu_DeleteComponent(NodeComponentViewModel? componentVM)
    {
        ((FsmNodeViewModel)componentVM!.Parent).DeleteComponent(componentVM);
    }

    private void NodeContextMenu_SetAsEndNode(FsmNodeViewModel? nodeVM)
    {
        nodeVM!.IsEndNode = !nodeVM.IsEndNode;
        nodeVM.UpdateBorderColor();
    }

    private void NodeContextMenu_EditNodeName(FsmNodeViewModel? nodeVM)
    {
        nodeVM!.IsRenaming = true;
    }

    private async Task NodeContextMenu_LinkToBaseFSM(FsmNodeViewModel nodeViewModel)
    {
        if (!string.IsNullOrEmpty(nodeViewModel.FsmName))
        {
            nodeViewModel.ClearBaseFsm();
            return;
        }

        var filesService = App.Current?.Services?.GetService<IFilesService>();
        if (filesService is null)
            return;

        var file = await filesService.OpenFileAsync("Select FSM to link to..", filters:
        [
            new FilePickerFileType("FSM File")
            {
                Patterns = ["*_fsm_ingame.msg", "*_fsm_ingame.json"]
            },
            FilePickerFileTypes.All,
        ]);
        if (file is null)
            return;

        IStorageFolder? parentFolder = await file.GetParentAsync();
        if (parentFolder is null)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "Could not determine parent folder.", ButtonEnum.Ok, Icon.Warning);
            await box.ShowWindowDialogAsync(App.Current.MainWindow);
            return;
        }

        string className = parentFolder.Name;
        string fsmName = Path.GetFileNameWithoutExtension(file.Name);

        if (!fsmName.StartsWith($"{className}"))
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Warning", "FSM File Name must start with the the folder/class name. " +
                "Example: if the folder was 'playerai', the file must start with 'playerai_'.", ButtonEnum.Ok, Icon.Warning);
            await box.ShowWindowDialogAsync(App.Current.MainWindow);
            return;
        }

        int end = fsmName.IndexOf("_fsm_ingame");
        fsmName = fsmName.AsSpan(className.Length + 1, end - (className.Length + 1)).ToString();

        nodeViewModel.SetBaseFsm(className, fsmName);
    }

    private void NodeContextMenu_ConnectionSelected(FsmConnectionViewModel graphConnection)
    {
        WeakReferenceMessenger.Default.Send(new EditConnectionRequest(graphConnection));
    }

    private void NodeContextMenu_DeleteConnection(FsmConnectionViewModel graphConnection)
    {
        var editor = (FsmEditorViewModel)this.DataContext!;
        editor.RemoveConnection(graphConnection);
    }

    private void NodeContextMenu_BringNodeIntoView(NodeViewModel? nodeVM)
    {
        WeakReferenceMessenger.Default.Send(new BringFsmNodeIntoViewRequest(nodeVM!));
    }

    private Flyout? _searchFlyout;
    private void NodeContextMenu_AddComponent(FsmNodeView? nodeView)
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

    private void BuildActionSearchMenu(FsmNodeView nodeView)
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

        if (searchVM.Context is not FsmNodeViewModel nodeViewModel)
            return;

        var component = Activator.CreateInstance(type);
        if (component is not BehaviorTreeComponent btComponent)
            return;

        var editor = (FsmEditorViewModel)this.DataContext!;
        btComponent.ParentGuid = nodeViewModel.Guid;
        btComponent.Guid = editor.GetNewGuid();
        if (btComponent is QuestActionComponent questActionComponent)
        {
            if (editor.HasQuestContext())
            {
                QuestContext questContext = editor.GetQuestContext()!;
                questActionComponent.Category = questContext.Category;
                questActionComponent.Subcategory = questContext.SubCategory;
                questActionComponent.Index = questContext.Index;
                questActionComponent.ProgressIndex = questContext.ProgressIndex;
                questActionComponent.ProgressHash = questContext.ProgressHash;
            }
        }
        else if (btComponent is QuestConditionComponent questConditionComponent)
        {
            if (editor.HasQuestContext())
            {
                QuestContext questContext = editor.GetQuestContext()!;
                questConditionComponent.Category = questContext.Category;
                questConditionComponent.Subcategory = questContext.SubCategory;
                questConditionComponent.Index = questContext.Index;
                questConditionComponent.ProgressIndex = questContext.ProgressIndex;
                questConditionComponent.ProgressHash = questContext.ProgressHash;
            }
        }

        editor.RegisterFsmElementGuid(btComponent.Guid, btComponent);
        nodeViewModel.AddComponent(btComponent);

        // Make sure to select it.
        WeakReferenceMessenger.Default.Send(new FsmComponentSelectedMessage(btComponent));
    }

    private void NodeContextMenu_SetBranch(FsmNodeViewModel nodeViewModel)
    {
        nodeViewModel.IsBranch = !nodeViewModel.IsBranch;
    }

    private void NodeContextMenu_CreateSelfTransition(FsmNodeViewModel nodeViewModel)
    {
        if (nodeViewModel.HasSelfTransition)
        {
            nodeViewModel.HasSelfTransition = false;

            TransitionViewModel transition = nodeViewModel.Transitions.FirstOrDefault(e => e.Source == nodeViewModel && e.Target == nodeViewModel)!;

            FsmConnectionViewModel fsmParentConnection = (FsmConnectionViewModel)transition.ParentConnection;
            fsmParentConnection.Transitions.Remove(transition);
            nodeViewModel.Transitions.Remove(transition);

            var editorVm = (FsmEditorViewModel)Editor.DataContext!;
            foreach (var transitionComponent in transition.ConditionComponents)
            {
                if (transitionComponent is TransitionConditionViewModel cond)
                    editorVm.UnregisterFsmElementGuid(cond.ConditionComponent.Guid); 
            }
        }
        else
        {
            nodeViewModel!.HasSelfTransition = true;

            var connection = new FsmConnectionViewModel() { Source = nodeViewModel, Target = nodeViewModel };
            var transition = new TransitionViewModel(connection)
            {
                Source = nodeViewModel,
                Target = nodeViewModel
            };
            connection.Transitions.Add(transition);
            nodeViewModel.Transitions.Add(transition);

            WeakReferenceMessenger.Default.Send(new EditConnectionRequest(connection));
        }
    }

    private void NodeContextMenu_DeleteNode(FsmNodeViewModel? node)
    {
        var editorVm = (FsmEditorViewModel)Editor.DataContext!;
        editorVm.RemoveNode(node!);
    }

    private async void NodeContextMenu_CopyGuid(FsmNodeView? nodeView)
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
            var groupVM = (FsmGroupNodeViewModel)groupingNode.DataContext!;

            var editorVm = (FsmEditorViewModel)this.DataContext!;

            ObservableCollection<MenuItemViewModel> items =
            [
                new MenuItemViewModel()
                {
                    Header = groupVM.Title,
                    FontWeight = FontWeight.Bold,
                    IconKind = "Material.Layers",
                    Enabled = true,
                    IsHitTestVisible = false, // Not greyed out but not clickable
                },
                MenuItemViewModel.Separator,
            ];
            items.Add(new MenuItemViewModel()
            {
                Header = "Add Node",
                IconKind = "Material.PlusBox",
                Enabled = true,
                Command = new RelayCommand(editorVm.AddNewNode),
            });
            items.Add(new MenuItemViewModel()
            {
                Header = $"Edit Layer Name",
                IconKind = "Material.Pencil",
                Enabled = true,
                Command = new RelayCommand<FsmGroupNodeViewModel>(GroupContextMenu_EditGroupName!),
                Parameter = groupVM,
            });
            items.Add(MenuItemViewModel.Separator);
            items.Add(new MenuItemViewModel()
            {
                Header = "Delete Layer",
                IconKind = "Material.LayersMinus",
                Enabled = true,
                Command = new RelayCommand<FsmGroupNodeViewModel>(GroupContextMenu_DeleteLayer!),
                Parameter = groupVM
            });

            var flyout = new MenuFlyout();
            flyout.ItemsSource = items;
            flyout.ShowAt(groupingNode, showAtPointer: true);

            e.Handled = true;
        }
    }

    private void GroupContextMenu_EditGroupName(FsmGroupNodeViewModel groupVm)
    {
        groupVm.IsRenaming = true;
    }

    private async void GroupContextMenu_DeleteLayer(FsmGroupNodeViewModel groupVm)
    {
        if (groupVm.Nodes.Count > 0)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Question", $"Delete layer? All nodes within the layer will be deleted.",
                MsBox.Avalonia.Enums.ButtonEnum.YesNo,
                icon: MsBox.Avalonia.Enums.Icon.Info);

            var res = await box.ShowWindowDialogAsync(App.Current.MainWindow);
            if (res != MsBox.Avalonia.Enums.ButtonResult.Yes)
                return;
        }

        var editorVm = (FsmEditorViewModel)this.DataContext!;
        editorVm.RemoveLayer(groupVm);
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

                int width = (connection.Transitions.Any() && connection.Transitions[0].ConditionComponents.Any()) ? 150 : 50;
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