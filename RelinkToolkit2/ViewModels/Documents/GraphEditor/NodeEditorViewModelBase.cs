using Avalonia;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

using GBFRDataTools.FSM.BehaviorTree;

using Nodify;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RelinkToolkit2.ViewModels.Documents.GraphEditor;

public partial class NodeEditorViewModelBase : EditorDocumentBase
{
    /// <summary>
    /// Lookup table for all elements with a guid (nodes, or components).
    /// </summary>
    protected readonly Dictionary<uint, object> _guidToElement = [];

    [ObservableProperty]
    private Point _viewportLocation;

    [ObservableProperty]
    private Size _viewportSize;

    [ObservableProperty]
    private double _viewportZoom = 1.0f;

    [ObservableProperty]
    private Point _contextMenuLocation;

    public Point MouseLocation { get; set; }

    public bool IsLayouted { get; set; } = false;

    /// <summary>
    /// Last name this file was saved as.
    /// </summary>
    public Uri? LastFile { get; set; }

    public override void RegisterMessageListeners()
    {
        WeakReferenceMessenger.Default.RegisterAll(this);
    }

    public override void UnregisterMessageListeners()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
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
        } while (_guidToElement.ContainsKey(guid));

        return guid;
    }
}
