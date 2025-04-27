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

using RelinkToolkit2.Messages.Fsm;
using RelinkToolkit2.ViewModels.Documents;
using RelinkToolkit2.ViewModels.Documents.GraphEditor;
using RelinkToolkit2.ViewModels.Documents.GraphEditor.Nodes;
using RelinkToolkit2.ViewModels.Documents.GraphEditor.TransitionComponents;

namespace RelinkToolkit2.ViewModels;

public partial class ConnectionEditorViewModel : Tool
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

            for (int i = 0; i < 5; i++)
            {
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
                    ConditionComponents = 
                    [
                        new TransitionConditionViewModel(new RecvSignal())
                        {
                            Title = "Condition 1",
                        },
                        new TransitionConditionOpViewModel() { Operand = TransitionOperandType.AND, Priority = 0 },
                        new TransitionConditionViewModel(new CheckChallengeMissionClear())
                        {
                            Title = "Condition 2",
                        },
                    ]
                });
            }
        }

        WeakReferenceMessenger.Default.Register<EditConnectionRequest>(this, (recipient, message) =>
        {
            Connection = message.Connection;
        });
    }

    [RelayCommand]
    public void OnNodeClicked(NodeViewModel node)
    {
        WeakReferenceMessenger.Default.Send(new NodeGraphSelectionChangeRequest(node));
        WeakReferenceMessenger.Default.Send(new BringFsmNodeIntoViewRequest(node)); // Highlight it on the graph
    }
}
