using Avalonia;
using Avalonia.Controls;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using Dock.Model.Mvvm.Controls;

using DynamicData.Tests;

using GBFRDataTools.FSM.BehaviorTree;

using Nodify;

using RelinkToolkit2.Messages;
using RelinkToolkit2.Messages.Fsm;
using RelinkToolkit2.Services;
using RelinkToolkit2.ViewModels.Documents.GraphEditor.Nodes;
using RelinkToolkit2.ViewModels.Windows;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RelinkToolkit2.ViewModels.Documents.GraphEditor;

public partial class NodeEditorViewModelBase : EditorDocumentBase
{
    protected readonly IDialogService _dialogService;
    protected readonly ComponentDocumentationService _componentDocumentation;

    public NodeEditorViewModelBase(IDialogService dialogService, ComponentDocumentationService componentDocumentation)
    {
        _dialogService = dialogService;
        _componentDocumentation = componentDocumentation;
    }

    /// <summary>
    /// Lookup table for all elements with a guid (nodes, or components).
    /// </summary>
    protected readonly Dictionary<uint, object> _guidToElement = [];

    [ObservableProperty]
    public partial Point ViewportLocation { get; set; }

    [ObservableProperty]
    public partial Size ViewportSize { get; set; }

    [ObservableProperty]
    public partial double ViewportZoom { get; set; } = 1.0f;

    [ObservableProperty]
    public partial Point ContextMenuLocation { get; set; }

    public Point MouseLocation { get; set; }

    public bool IsLayouted { get; set; } = false;

    /// <summary>
    /// Last name this file was saved as.
    /// </summary>
    public Uri? LastFile { get; set; }

    [RelayCommand]
    public static void OnDeleteComponent(NodeComponentViewModel? componentVM)
    {
        componentVM!.Parent.DeleteComponent(componentVM);
    }

    [RelayCommand]
    public static void OnCopyComponent(NodeComponentViewModel? componentVM)
    {
        var parentNode = componentVM!.Parent;
        var clonedComponent = componentVM.CloneAsNewComponent(parentNode);
        parentNode.Components.Add(clonedComponent);
    }

    [RelayCommand]
    public static void OnEditNodeName(NodeViewModelBase? nodeVM)
    {
        nodeVM!.IsRenaming = true;
    }

    [RelayCommand]
    public async Task OnEditComponentDocumentation(NodeComponentViewModel component)
    {
        if (component is null)
            return;

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
            bool isReadOnly = component.Parent is FsmNodeViewModel fsmNode && !fsmNode.IsEditableNode;
            WeakReferenceMessenger.Default.Send(new FsmComponentSelectedMessage(component.Component, isReadOnly));
        }
    }

    [RelayCommand]
    public async Task OnEditComponentCaption(NodeComponentViewModel component)
    {
        if (component is null)
            return;

        string key = ComponentDocumentationService.BuildTypeKey(component.Component.GetType());
        string? value = _componentDocumentation.GetComponentPreviewFormatString(key);

        var vm = new ComponentCaptionEditViewModel()
        {
            Key = key,
            Value = value,
            Component = component.Component,
        };
        await _dialogService.ShowDialogAsync(vm);

        if (vm.HasSaved)
        {
            _componentDocumentation.SetComponentPreviewFormatString(vm.Key, vm.Value);
            WeakReferenceMessenger.Default.Send(new SnackbarMessage($"Updated caption for '{vm.Key}'."));

            component.UpdateCaption();
        }
    }

    [RelayCommand]
    public void OnDeleteConnection(GraphConnectionViewModel graphConnection)
    {
        RemoveConnection(graphConnection);
    }

    [RelayCommand]
    public static void OnConnectionSelected(GraphConnectionViewModel graphConnection)
    {
        WeakReferenceMessenger.Default.Send(new EditConnectionRequest(graphConnection));
    }

    [RelayCommand]
    public static void OnBringNodeIntoView(NodeViewModel? nodeVM)
    {
        WeakReferenceMessenger.Default.Send(new BringNodeIntoViewRequest(nodeVM!));
    }

    [RelayCommand]
    public void OnDeleteNode(NodeViewModel? node)
    {
        if (node is null)
            return;

        RemoveNode(node);
    }

    [RelayCommand]
    public void OnDeleteNodes(IList<FsmNodeViewModelBase>? nodes)
    {
        if (nodes is null)
            return;

        for (int i = 0; i < nodes.Count; i++)
        {
            RemoveNode(nodes[i]);
        }
    }

    [RelayCommand]
    public void OnCopyAsNewNode(NodeViewModel nodeVm)
    {
        CloneAndAddNewNode(nodeVm);
    }

    public virtual void NodeClicked(NodeViewModel? node)
    {
        // HACK: This exists because clicking away from a flyout for a node doesn't dismiss the flyout for some reason..
        // Request the view (yikes!) to do so.
        if (node is null)
            WeakReferenceMessenger.Default.Send<DismissSearchComponentMenuRequest>();

        WeakReferenceMessenger.Default.Send(new NodeSelectedMessage(node));
    }

    public virtual void CloneAndAddNewNode(NodeViewModel nodeVm)
    {

    }

    /// <summary>
    /// Removes a connection from the graph.
    /// </summary>
    /// <param name="graphConnection"></param>
    public virtual void RemoveConnection(GraphConnectionViewModel graphConnection)
    {

    }

    public virtual void RemoveNode(NodeViewModel node)
    {
        
    }

    /// <summary>
    /// Generates a new guid.
    /// </summary>
    /// <returns></returns>
    public uint GetNewGuid()
    {
        uint guid;
        do
        {
            guid = (uint)Random.Shared.Next();
        } while (_guidToElement.ContainsKey(guid));

        return guid;
    }
}
