using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.PropertyGrid.Controls;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using Dock.Model.Mvvm.Controls;

using Microsoft.Extensions.DependencyInjection;

using ReactiveUI;

using RelinkToolkit2.Controls.PropertyGrid.ViewModel;
using RelinkToolkit2.Messages;
using RelinkToolkit2.Messages.Fsm;
using RelinkToolkit2.Services;
using RelinkToolkit2.ViewModels.Documents.GraphEditor;
using RelinkToolkit2.ViewModels.Documents.GraphEditor.Nodes;
using RelinkToolkit2.ViewModels.Properties;
using RelinkToolkit2.ViewModels.Windows;

namespace RelinkToolkit2.ViewModels.Tools.Properties;

public partial class FsmNodePropertiesEditorViewModel : NodePropertiesViewModelBase,
    IRecipient<NodeSelectedMessage>
{
    public const string GlobalId = "FSMNodeProperties";

    [ObservableProperty]
    public partial FsmNodeViewModel? Node { get; set; }

    public FsmNodePropertiesEditorViewModel(ComponentDocumentationService componentDocumentation, IDialogService dialogService)
        : base(componentDocumentation, dialogService)
    { 
        Id = GlobalId;
        Title = "FSM Node Properties";

        PropertyGrid = App.Current.Services.GetRequiredService<DocumentablePropertyGridViewModel>();

        if (Design.IsDesignMode)
        {
            var parentEditor = App.Current.Services.GetRequiredService<FsmEditorViewModel>();
            Node = new FsmNodeViewModel()
            {
                Title = "Test Node",
                FsmSource = "TestFSM",
                IsBranch = true,
                ParentEditor = parentEditor,
            };
        }
    }

    public override void OnActive()
    {
        base.OnActive();

        Node = null;
        PropertyGrid.SetObject(null);
    }

    public void Receive(NodeSelectedMessage message)
    {
        Node = (FsmNodeViewModel)message.Node;
        PropertyGrid.SetObject(Node?.Components.FirstOrDefault()?.Component);
    }
}
