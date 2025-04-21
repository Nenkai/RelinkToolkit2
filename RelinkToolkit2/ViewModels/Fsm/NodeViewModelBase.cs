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

namespace RelinkToolkit2.ViewModels.Fsm;

/// <summary>
/// Base node on a graph. Can be a node or a group.
/// </summary>
public partial class NodeViewModelBase : ObservableObject
{
    /// <summary>
    /// Editor to which this node belongs to.
    /// </summary>
    public required FsmEditorViewModel ParentEditor { get; set; }

    /// <summary>
    /// Location of the node (top left). For center, refer to <see cref="Center"/>
    /// </summary>
    [ObservableProperty]
    private Point _location;

    /// <summary>
    /// Size of the node.
    /// </summary>
    [ObservableProperty]
    private Size _size;

    /// <summary>
    /// Layer for this node.
    /// </summary>
    [ObservableProperty]
    private int _layerIndex;

    /// <summary>
    /// Title/Name.
    /// </summary>
    [ObservableProperty]
    private string? _title;

    [ObservableProperty]
    private bool _isRenaming;

    /// <summary>
    /// Center location. For top right, refer to <see cref="Location"/>
    /// </summary>
    public Point Center => new(Location.X + (Size.Width / 2), Location.Y + (Size.Height / 2));

    /// <summary>
    /// Boundary box.
    /// </summary>
    public Rect BoundaryBox => new(Location, Size);

    public Rect CollisionBox => BoundaryBox.Deflate(new Thickness(Size.Width * 0.25, Size.Height * 0.25));

    partial void OnLayerIndexChanged(int value)
    {
        if (this is NodeViewModel node)
            WeakReferenceMessenger.Default.Send(new FsmNodeLayerChangedMessage(this), node.Guid);
    }

    public static bool AreIntersected(Rect r1, Rect r2)
    {
        return (r2.X < r1.X + r1.Width) &&
            (r1.X < (r2.X + r2.Width)) &&
            (r2.Y < r1.Y + r1.Height) &&
            (r1.Y < r2.Y + r2.Height);
    }
}

