using System;
using System.Linq;

using CommunityToolkit.Mvvm.Messaging;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Nodify;

using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Core.Layout;

using RelinkToolkit2.Messages.Fsm;
using RelinkToolkit2.ViewModels.Fsm;
using RelinkToolkit2.ViewModels.Documents;
using System.Collections.Generic;
using System.Xml.Linq;

namespace RelinkToolkit2.Views.Documents;

public partial class FsmEditorView : UserControl
{
    public FsmEditorView()
    {
        WeakReferenceMessenger.Default.Register<NodeBringIntoViewRequest>(this, (recipient, message) =>
        {
            Editor.BringIntoView(message.Node.Location, animated: true);
            message.Reply(true);
        });

        WeakReferenceMessenger.Default.Register<GetNodeControlRequest>(this, (recipient, message) =>
        {
            Control control = Editor.ContainerFromItem(message.Node)!;
            message.Reply(control);
        });

        InitializeComponent();
    }

    /// <summary>
    /// Fired when nodes are loaded.
    /// Any automatic node layouting happens here.
    /// </summary>
    private void EditorViewModel_NodesLoaded()
    {
        // Ensure to update everything
        Editor.UpdateLayout();

        if (Editor.Items.Any() && Editor.Tag as bool? != true)
        {
            // Nodes are now loaded. Proceed to create an automated layout
            var graph = new GeometryGraph();
            foreach (object? obj in Editor.Items)
            {
                if (obj is not NodeViewModel nodeViewModel)
                    continue;

                Control? control = Editor.ContainerFromItem(nodeViewModel);

                double width = 100; double height = 100;
                if (control is not null)
                {
                    width = control.DesiredSize.Width;
                    height = control.DesiredSize.Height;
                }

                var node = new Microsoft.Msagl.Core.Layout.Node(CreateCurve(width, height), nodeViewModel);
                graph.Nodes.Add(node);
            }

            foreach (ConnectionViewModel connection in Editor.Connections)
            {
                int width = (connection.Transitions.Any() && connection.Transitions[0].ConditionComponents.Any()) ? 150 : 50;
                Edge edge = new(graph.FindNodeByUserData(connection.Source), graph.FindNodeByUserData(connection.Target), width, 30, 20);
                graph.Edges.Add(edge);
            }

            var settings = new SugiyamaLayoutSettings
            {
                Transformation = PlaneTransformation.Rotation(Math.PI / 2), // Make LR (Left-To-Right)
                NodeSeparation = 80,
                EdgeRoutingSettings = { EdgeRoutingMode = EdgeRoutingMode.StraightLine },
            };

            var layout = new LayeredLayout(graph, settings);
            layout.Run();

            foreach (var graphNode in graph.Nodes)
            {
                NodeViewModel nvm = (NodeViewModel)graphNode.UserData;

                // God, thanks wolvenkit for actually figuring out how to lay down the stuff
                // https://github.com/WolvenKit/WolvenKit/blob/5a3449a6880350bf7164911e735adf6003fe6981/WolvenKit.App/ViewModels/Documents/RDTGraphViewModel.cs#L192
                // Probably basic maths but I had literally NOT A SINGLE CLUE I had to do further center maths calcs for the node location
                nvm.Location = new Point(graphNode.Center.X - graph.BoundingBox.Center.X - graphNode.Width / 2,
                                         graphNode.Center.Y - graph.BoundingBox.Center.Y - graphNode.Height / 2);
            }

            if (graph.Nodes.Any())
            {
                var firstNode = (NodeViewModel)graph.Nodes[0].UserData;
                Editor.BringIntoView(firstNode.Location, animated: false);
            }

            // Bad. I know i'm not supposed to get the VM from the view
            // But how else am I gonna deal with doing the grouping after layouting?
            var editorVm = (FsmEditorViewModel)Editor.DataContext!;

            Dictionary<int, GroupNodeViewModel> groupNodes = [];
            for (int i = editorVm.Nodes.Count - 1; i >= 0; i--)
            {
                NodeViewModelBase node = editorVm.Nodes[i];
                if (node is not NodeViewModel nvm)
                    continue;

                if (nvm.LayerIndex != 0)
                {
                    if (!groupNodes.TryGetValue(nvm.LayerIndex, out GroupNodeViewModel groupNode))
                    {
                        groupNode = new();
                        groupNodes.Add(nvm.LayerIndex, groupNode);

                        // TODO: Fix cast exception. FsmEditorView.axaml's ItemContainerStyle needs to be cleaned up 
                        editorVm.Nodes.Add(groupNode);

                        groupNode.Location = nvm.Location;
                        groupNode.Size = nvm.Size;
                        groupNode.Title = $"Layer {nvm.LayerIndex}";
                    }

                    Control? control = Editor.ContainerFromItem(nvm);

                    double minX = groupNode.Location.X;
                    double minY = groupNode.Location.Y;
                    double maxX = groupNode.Location.X + groupNode.Size.Width;
                    double maxY = groupNode.Location.Y + groupNode.Size.Height;

                    if (nvm.Location.X < minX)
                        minX = nvm.Location.X;

                    if (nvm.Location.Y < minY)
                        minY = nvm.Location.Y;

                    var sizeX = nvm.Location.X + control.DesiredSize.Width;
                    if (sizeX > maxX)
                        maxX = sizeX;

                    var sizeY = nvm.Location.Y + control.DesiredSize.Height;
                    if (sizeY > maxY)
                        maxY = sizeY;

                    var result = new Rect(minX, minY, maxX - minX, maxY - minY);
                    groupNode.Location = new Point(minX, minY);
                    groupNode.Size = result.Size;
                }
            }

            const int VerticalPadding = 30; // 30 (group header height more or less)
            const int padding = 15;
            foreach (var group in groupNodes)
            {
                int vPad = padding + VerticalPadding;
                group.Value.Location = new Point(group.Value.Location.X - padding, group.Value.Location.Y - vPad);
                group.Value.Size = new Size(group.Value.Size.Width + (padding * 2), group.Value.Size.Height + (vPad * 2));
            }

            Editor.Tag = true;
        }
    }

    public static ICurve CreateCurve(double w, double h)
    {
        return CurveFactory.CreateRectangle(w, h, new Microsoft.Msagl.Core.Geometry.Point(w, h));
    }

    private void NodifyEditor_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        EditorViewModel_NodesLoaded();
    }
}