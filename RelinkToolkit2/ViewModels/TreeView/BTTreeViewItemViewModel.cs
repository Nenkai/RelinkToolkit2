using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using RelinkToolkit2.Messages;
using RelinkToolkit2.Messages.Documents;
using RelinkToolkit2.ViewModels.Documents;
using RelinkToolkit2.ViewModels.Documents.GraphEditor;

namespace RelinkToolkit2.ViewModels.TreeView;

public partial class BTTreeViewItemViewModel : TreeViewItemViewModel
{
    public required BTEditorViewModel FsmEditor { get; set; }

    public BTTreeViewItemViewModel()
    {
        IconKind = "Material.Graph";
        DoubleClickedCommand = new RelayCommand(OnDoubleClicked);
    }

    public void OnDoubleClicked()
    {
        WeakReferenceMessenger.Default.Send(new OpenDocumentRequest(FsmEditor));
    }
}
