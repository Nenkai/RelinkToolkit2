using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using GBFRDataTools.FSM;

using RelinkToolkit2.Messages;
using RelinkToolkit2.Messages.Documents;
using RelinkToolkit2.Messages.Fsm;
using RelinkToolkit2.ViewModels.Documents.GraphEditor.Nodes;

namespace RelinkToolkit2.ViewModels.TreeView;

public partial class FSMLayerTreeViewItemViewModel : TreeViewItemViewModel
{
    public required FsmGroupNodeViewModel LayerGroup { get; set; }

    public FSMLayerTreeViewItemViewModel()
    {
        IconKind = "Material.Layers";
        DoubleClickedCommand = new RelayCommand(OnDoubleClicked);
    }

    public void OnDoubleClicked()
    {
        WeakReferenceMessenger.Default.Send(new OpenDocumentRequest(LayerGroup.ParentEditor));
        WeakReferenceMessenger.Default.Send(new BringFsmNodeIntoViewRequest(LayerGroup, animated: false));
    }
}
