using Avalonia.Controls;
using Avalonia.PropertyGrid.Controls;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using Dock.Model.Mvvm.Controls;

using Microsoft.Extensions.DependencyInjection;

using RelinkToolkit2.Controls.PropertyGrid.ViewModel;
using RelinkToolkit2.Messages;
using RelinkToolkit2.Messages.Fsm;
using RelinkToolkit2.Services;
using RelinkToolkit2.ViewModels.Documents.GraphEditor;
using RelinkToolkit2.ViewModels.Documents.GraphEditor.Nodes;
using RelinkToolkit2.ViewModels.Properties;
using RelinkToolkit2.ViewModels.Windows;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RelinkToolkit2.ViewModels.Tools.Properties;

public partial class BtNodePropertiesEditorViewModel : NodePropertiesViewModelBase,
    IRecipient<NodeSelectedMessage>
{
    public const string GlobalId = "BTNodeProperties";

    [ObservableProperty]
    public partial BTNodeViewModel? Node { get; set; }

    public BtNodePropertiesEditorViewModel(ComponentDocumentationService componentDocumentation, IDialogService dialogService)
        : base(componentDocumentation, dialogService)
    {
        Id = GlobalId;
        Title = "Behavior Tree Node Properties";

        PropertyGrid = App.Current.Services.GetRequiredService<DocumentablePropertyGridViewModel>();

        if (Design.IsDesignMode)
        {
            var parentEditor = App.Current.Services.GetRequiredService<BTEditorViewModel>();
            Node = new BTNodeViewModel()
            {
                Title = "Test Node",
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
        Node = (BTNodeViewModel)message.Node;
        PropertyGrid.SetObject(Node?.Components.FirstOrDefault()?.Component);
    }
}
