using Avalonia.Controls;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using Dock.Model.Mvvm.Controls;

using GBFRDataTools.FSM.Components.Conditions.Quest;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using RelinkToolkit2.Messages.Fsm;
using RelinkToolkit2.Services.Configuration.Models;
using RelinkToolkit2.ViewModels.Documents.GraphEditor;
using RelinkToolkit2.ViewModels.Documents.GraphEditor.Nodes;
using RelinkToolkit2.ViewModels.Documents.GraphEditor.TransitionComponents;

namespace RelinkToolkit2.ViewModels.Tools;

public partial class ConnectionEditorViewModel : Tool,
    IRecipient<EditConnectionRequest>
{
    private readonly IOptions<FSMOptions> _fsmOptions;

    [ObservableProperty]
    public partial GraphConnectionViewModel? Connection { get; set; }

    public ConnectionEditorViewModel() { }

    public ConnectionEditorViewModel(IOptions<FSMOptions> fsmOptions) 
    {
        _fsmOptions = fsmOptions;

        Id = "ConnectionEditor";
        Title = "Connection Editor";

        if (Design.IsDesignMode)
        {
            var editor = App.Current.Services.GetRequiredService<FsmEditorViewModel>();
            Connection= new FsmConnectionViewModel()
            {
                Source = new FsmNodeViewModel() { ParentEditor = editor, },
                Target = new FsmNodeViewModel() { ParentEditor = editor, },
            };

            var fsmConnection = (FsmConnectionViewModel)Connection;
            var parentOperator = new TransitionConditionOpViewModel(fsmConnection)
                .AddElement(new TransitionConditionViewModel(new RecvSignal()) { Title = "RecvSignal" })
                .AddElement(new TransitionConditionViewModel(new CheckAnyHaveBahaWeapon()) { Title = "CheckAnyHaveBahaWeapon" })
                .AddElement(new TransitionConditionOpViewModel(fsmConnection, null, TransitionOperandType.AND)
                    .AddElement(new TransitionConditionViewModel(new RecvSignal()) { Title = "RecvSignal" })
                )
                .AddElement(new TransitionConditionViewModel(new CheckChallengeMissionClear()) { Title = "CheckChallengeMissionClear" });
            Connection.Transitions.Add(new TransitionViewModel(fsmConnection)
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
            Connection.Transitions.Add(new TransitionViewModel(fsmConnection)
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
                    new TransitionConditionOpViewModel(fsmConnection)
                        .AddElement(new TransitionConditionViewModel(new RecvSignal()) { Title = "RecvSignal" })
                ]
            });
        }

        WeakReferenceMessenger.Default.RegisterAll(this);
    }

    public void Receive(EditConnectionRequest message)
    {
        Connection = message.Connection;

        // Set whether conditions are expanded depending on config
        if (Connection is not null)
        {
            foreach (var transition in Connection.Transitions)
            {
                foreach (var component in transition.ConditionOperatorGroups)
                {
                    SetExpandProperties(component);
                }
            }
        }
    }

    private void SetExpandProperties(TransitionConditionOpViewModel operatorGroup)
    {
        foreach (var element in operatorGroup.Elements)
        {
            if (element is TransitionConditionViewModel item)
            {
                item.IsExpanded = _fsmOptions.Value.AlwaysExpandConditions;
            }
            else if (element is TransitionConditionOpViewModel subOperatorGroup)
            {
                SetExpandProperties(subOperatorGroup);
            }
        }
    }

    [RelayCommand]
    public void OnNodeClicked(NodeViewModel node)
    {
        WeakReferenceMessenger.Default.Send(new NodeGraphSelectionChangeRequest(node));
        WeakReferenceMessenger.Default.Send(new BringNodeIntoViewRequest(node)); // Highlight it on the graph
    }
}
