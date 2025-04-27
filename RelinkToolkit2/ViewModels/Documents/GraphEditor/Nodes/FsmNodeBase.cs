using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

using RelinkToolkit2.Messages.Fsm;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RelinkToolkit2.ViewModels.Documents.GraphEditor.Nodes;

/// <summary>
/// Represents a node for FSM. Can be a node or a group.
/// </summary>
public abstract partial class FsmNodeViewModelBase : NodeViewModel
{
    [ObservableProperty]
    private int _layerIndex;

    partial void OnLayerIndexChanged(int value)
    {
        if (this is FsmNodeViewModel node)
            WeakReferenceMessenger.Default.Send(new FsmNodeLayerChangedMessage(this), node.Guid);
    }
}
