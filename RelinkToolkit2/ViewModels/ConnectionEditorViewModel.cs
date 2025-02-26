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

using RelinkToolkit2.Messages;
using RelinkToolkit2.Messages.Fsm;
using RelinkToolkit2.ViewModels.Fsm;
using RelinkToolkit2.ViewModels.Fsm.TransitionComponents;

namespace RelinkToolkit2.ViewModels;

public partial class ConnectionEditorViewModel : Tool
{
    [ObservableProperty]
    private ConnectionViewModel? _connection;

    public ConnectionEditorViewModel() 
    {
        Id = "ConnectionEditor";
        Title = "Connection Editor";

        if (Design.IsDesignMode)
        {
            _connection = new ConnectionViewModel();
            for (int i = 0; i < 5; i++)
            {
                _connection.Transitions.Add(new TransitionViewModel(_connection)
                {
                    Source = new()
                    {
                        Title = "Source",
                    },
                    Target = new()
                    {
                        Title = "Target"
                    },
                    ConditionComponents = 
                    [
                        new TransitionConditionViewModel(new RecvSignal())
                        {
                            Title = "Condition 1",
                        },
                        new TransitionConditionOpViewModel() { Title = "AND", Priority = 0 },
                        new TransitionConditionViewModel(new CheckChallengeMissionClear())
                        {
                            Title = "Condition 2",
                        },
                    ]
                });
            }
        }

        WeakReferenceMessenger.Default.Register<ConnectionSelectionChangedMessage>(this, (recipient, message) =>
        {
            Connection = message.Value;
        });
    }

    [RelayCommand]
    public void OnNodeClicked(NodeViewModel node)
    {
        WeakReferenceMessenger.Default.Send(new NodeGraphSelectionChangeRequest(node));
        WeakReferenceMessenger.Default.Send(new NodeBringIntoViewRequest(node)); // Highlight it on the graph
    }
}
