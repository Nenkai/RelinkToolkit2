using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Media;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

using RelinkToolkit2.Messages.Fsm;
using RelinkToolkit2.ViewModels.Documents;

namespace RelinkToolkit2.ViewModels.Documents.GraphEditor.Nodes;

/// <summary>
/// Base node on a graph.
/// </summary>
public partial class NodeViewModelBase : ObservableObject
{
    /// <summary>
    /// Default node name.
    /// </summary>
    public const string DefaultNodeName = "FSMNode";

    /// <summary>
    /// How much the collision bounds of layers extends past their visible bounds
    /// 0.25 = 25% of the layer's size.
    /// </summary>
    public const double NodeBoundaryBoxExtendsRatio = 0.25;

    /// <summary>
    /// Editor to which this node belongs to.
    /// </summary>
    public required NodeEditorViewModelBase ParentEditor { get; set; }

    /// <summary>
    /// Location of the node (top left). For center, refer to <see cref="Center"/>
    /// </summary>
    [ObservableProperty]
    public partial Point Location { get; set; }

    /// <summary>
    /// Size of the node.
    /// </summary>
    [ObservableProperty]
    public partial Size Size { get; set; }

    /// <summary>
    /// Title/Name.
    /// </summary>
    [ObservableProperty]
    public partial string? Title { get; set; }

    [ObservableProperty]
    public partial bool IsRenaming { get; set; }

    /// <summary>
    /// Center location. For top right, refer to <see cref="Location"/>
    /// </summary>
    public Point Center => new(Location.X + (Size.Width / 2), Location.Y + (Size.Height / 2));

    /// <summary>
    /// Boundary box.
    /// </summary>
    public Rect BoundaryBox => new(Location, Size);

    public Rect CollisionBox => BoundaryBox.Deflate(new Thickness(Size.Width * NodeBoundaryBoxExtendsRatio, Size.Height * NodeBoundaryBoxExtendsRatio));

    partial void OnIsRenamingChanged(bool value)
    {
        if (!value && string.IsNullOrWhiteSpace(Title))
            Title = NodeViewModel.DefaultNodeName;
    }

    public static bool AreIntersected(Rect r1, Rect r2)
    {
        return (r2.X < r1.X + r1.Width) &&
            (r1.X < (r2.X + r2.Width)) &&
            (r2.Y < r1.Y + r1.Height) &&
            (r1.Y < r2.Y + r2.Height);
    }
}

