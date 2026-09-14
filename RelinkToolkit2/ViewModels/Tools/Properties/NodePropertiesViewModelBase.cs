using Avalonia.PropertyGrid.Controls;

using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using Dock.Model.Mvvm.Controls;

using ReactiveUI;

using RelinkToolkit2.Controls.PropertyGrid.ViewModel;
using RelinkToolkit2.Messages;
using RelinkToolkit2.Messages.Fsm;
using RelinkToolkit2.Services;
using RelinkToolkit2.ViewModels.Documents.GraphEditor;
using RelinkToolkit2.ViewModels.Documents.GraphEditor.Nodes;
using RelinkToolkit2.ViewModels.Tools;
using RelinkToolkit2.ViewModels.Windows;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RelinkToolkit2.ViewModels.Properties;

public partial class NodePropertiesViewModelBase : ToolBase,
    IRecipient<FsmComponentSelectedMessage>
{
    protected readonly ComponentDocumentationService _componentDocumentation;
    protected readonly IDialogService _dialogService;

    public DocumentablePropertyGridViewModel PropertyGrid { get; set; }

    public NodePropertiesViewModelBase(ComponentDocumentationService componentDocumentation, IDialogService dialogService)
    {
        _componentDocumentation = componentDocumentation;
        _dialogService = dialogService;
    }

    [RelayCommand]
    public void OnAddComponent(FsmNodeViewModel node)
    {
        WeakReferenceMessenger.Default.Send(new BringNodeIntoViewRequest(node));
    }

    [RelayCommand]
    public void OnComponentDeleted(NodeComponentViewModel component)
    {
        component.Parent.DeleteComponent(component);
    }

    [RelayCommand]
    public void OnComponentSelected(NodeComponentViewModel component)
    {
        PropertyGrid.SetObject(component.Component);
    }

    [RelayCommand]
    public void OnBringNodeIntoView(FsmNodeViewModel node)
    {
        WeakReferenceMessenger.Default.Send(new BringNodeIntoViewRequest(node));
    }

    [RelayCommand]
    public async Task OnComponentDocumentation(NodeComponentViewModel component)
    {
        string key = ComponentDocumentationService.BuildTypeKey(component.Component.GetType());
        string? value = _componentDocumentation.GetComponentString(key);

        var vm = new ComponentDocEditViewModel()
        {
            Key = key,
            Value = value,
        };
        await _dialogService.ShowDialogAsync(vm);

        if (vm.HasSaved)
        {
            _componentDocumentation.SetComponentString(vm.Key, vm.Value);
            WeakReferenceMessenger.Default.Send(new SnackbarMessage($"Updated '{vm.Key}'."));

            // Force a re-select to update the description
            PropertyGrid.SetObject(component.Component);
        }
    }

    public void Receive(FsmComponentSelectedMessage message)
    {
        PropertyGrid.ReadOnly = message.ReadOnly;
        PropertyGrid.SetObject(message.Component);

    }
}
