using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using Avalonia.Controls;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using Dock.Model.Mvvm.Controls;

using GBFRDataTools.FSM.Components.Conditions.Quest;
using GBFRDataTools.FSM.Components.Conditions.UI;

using RelinkToolkit2.Messages.Fsm;
using RelinkToolkit2.ViewModels.Documents;
using RelinkToolkit2.ViewModels.Documents.GraphEditor;
using RelinkToolkit2.ViewModels.Documents.GraphEditor.Nodes;
using RelinkToolkit2.ViewModels.Documents.GraphEditor.TransitionComponents;

namespace RelinkToolkit2.ViewModels;

public partial class ConnectionEditorViewModel : Tool,
    IRecipient<EditConnectionRequest>
{
    [ObservableProperty]
    private FsmConnectionViewModel? _connection;

    public ConnectionEditorViewModel() 
    {
        Id = "ConnectionEditor";
        Title = "Connection Editor";

        if (Design.IsDesignMode)
        {
            var editor = new FsmEditorViewModel();
            _connection = new FsmConnectionViewModel()
            {
                Source = new FsmNodeViewModel() { ParentEditor = editor, },
                Target = new FsmNodeViewModel() { ParentEditor = editor, },
            };

            var parentOperator = new TransitionConditionOpViewModel(_connection)
                .AddElement(new TransitionConditionViewModel(new RecvSignal()) { Title = "RecvSignal" })
                .AddElement(new TransitionConditionViewModel(new CheckAnyHaveBahaWeapon()) { Title = "CheckAnyHaveBahaWeapon" })
                .AddElement(new TransitionConditionOpViewModel(_connection, null, TransitionOperandType.AND)
                    .AddElement(new TransitionConditionViewModel(new RecvSignal()) { Title = "RecvSignal" })
                )
                .AddElement(new TransitionConditionViewModel(new CheckChallengeMissionClear()) { Title = "CheckChallengeMissionClear" });

            _connection.Transitions.Add(new TransitionViewModel(_connection)
            {
                Source = new()
                {
                    ParentEditor = editor,
                    Title = "Source",
                },
                Target = new()
                {
                    ParentEditor = editor,
                    Title = "Target"
                },
                ConditionOperatorGroups = [parentOperator]
            });

            _connection.Transitions.Add(new TransitionViewModel(_connection)
            {
                Source = new()
                {
                    ParentEditor = editor,
                    Title = "Target",
                },
                Target = new()
                {
                    ParentEditor = editor,
                    Title = "Source"
                },
                ConditionOperatorGroups =
                [
                    new TransitionConditionOpViewModel(_connection)
                        .AddElement(new TransitionConditionViewModel(new RecvSignal()) { Title = "RecvSignal" })
                ]
            });
        }

        WeakReferenceMessenger.Default.RegisterAll(this);
    }

    public void Receive(EditConnectionRequest message)
    {
        Connection = message.Connection;
    }

    [RelayCommand]
    public void OnNodeClicked(NodeViewModel node)
    {
        WeakReferenceMessenger.Default.Send(new NodeGraphSelectionChangeRequest(node));
        WeakReferenceMessenger.Default.Send(new BringNodeIntoViewRequest(node)); // Highlight it on the graph
    }
}
