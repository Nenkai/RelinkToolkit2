using Avalonia;
using Avalonia.Controls;
using Avalonia.PropertyGrid.Controls;

using AvaloniaDialogs.Views;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using Dock.Model.Mvvm.Controls;

using DynamicData.Aggregation;

using GBFRDataTools.FSM.Components.Actions.Quest;

using Microsoft.Extensions.DependencyInjection;

using ReactiveUI;

using RelinkToolkit2.Messages;
using RelinkToolkit2.Messages.Fsm;
using RelinkToolkit2.Services;
using RelinkToolkit2.ViewModels.Windows;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RelinkToolkit2.Controls.PropertyGrid.ViewModel;

public partial class DocumentablePropertyGridViewModel : ObservableObject
{
    private readonly IDialogService _dialogService;
    private readonly ComponentDocumentationService _componentDocService;

    [ObservableProperty]
    public partial object? SelectedObject { get; private set; }

    [ObservableProperty]
    public partial string? ObjectName { get; set; }

    [ObservableProperty]
    public partial string? Description { get; set; }

    private string _objectTitle;
    private string _objectDescription;

    public DocumentablePropertyGridViewModel(IDialogService dialogService,
        ComponentDocumentationService componentDocumentationService)
    {
        _dialogService = dialogService;
        _componentDocService = componentDocumentationService;

        if (Design.IsDesignMode)
        {
            SelectedObject = new BeginSection();
            UpdateWithComponentName(SelectedObject);
        }
    }

    public void SetObject(object? obj)
    {
        SelectedObject = obj;
        UpdateWithComponentName(SelectedObject);
    }

    private void UpdateWithComponentName(object obj)
    {
        if (obj is not null)
        {
            var type = obj.GetType();
            ObjectName = type.Name;

            string key = ComponentDocumentationService.BuildTypeKey(type);
            string? desc = _componentDocService.GetComponentString(key);
            Description = desc ?? GetNoDocMessage(type.Name);
            _objectTitle = ObjectName;
            _objectDescription = Description;
        }
        else
        {
            ObjectName = string.Empty;
            Description = string.Empty;
        }
    }

    [RelayCommand]
    public void OnPropertyGotFocus(PropertyGotFocusEventArgs @event)
    {
        string key = _componentDocService.BuildPropertyKey(@event.Context.Property);
        string? desc = _componentDocService.GetPropertyString(key);

        ObjectName = @event.Context.Property.Name;
        Description = !string.IsNullOrWhiteSpace(desc) ? desc : GetNoPropertyDocMessage(@event.Context.Property.Name);
    }

    [RelayCommand]
    public void OnPropertyLostFocus(PropertyLostFocusEventArgs @event)
    {
        ObjectName = _objectTitle;
        Description = _objectDescription;
    }

    [RelayCommand]
    public void OnPointerEnteredPropertyName(PropertyCellContext context)
    {
        ObjectName = context.Property.Name;
        JsonPropertyNameAttribute? jsonPropNameAttr = context.Property.Attributes.OfType<JsonPropertyNameAttribute>().FirstOrDefault();
        if (jsonPropNameAttr is not null)
            ObjectName += $" ({jsonPropNameAttr.Name})";

        string key = _componentDocService.BuildPropertyKey(context.Property);
        string? desc = _componentDocService.GetPropertyString(key);

        Description = !string.IsNullOrWhiteSpace(desc) ? desc : GetNoPropertyDocMessage(context.Property.Name);
    }


    [RelayCommand]
    public void OnPointerExitedPropertyName(PropertyCellContext context)
    {
        ObjectName = _objectTitle;
        Description = _objectDescription;
    }

    [RelayCommand]
    public async Task OnEditPropertyDocumentation(PropertyDescriptor? propertyDescriptor)
    {
        if (propertyDescriptor is null)
            return;

        string key = _componentDocService.BuildPropertyKey(propertyDescriptor);
        string? value = _componentDocService.GetPropertyString(key);

        var vm = new ComponentDocEditViewModel()
        {
            Key = key,
            Value = value,
        };
        await _dialogService.ShowDialogAsync(vm);

        if (vm.HasSaved)
        {
            _componentDocService.SetPropertyString(vm.Key, vm.Value);
            WeakReferenceMessenger.Default.Send(new SnackbarMessage($"Updated '{vm.Key}'."));
        }
    }

    private static string GetNoPropertyDocMessage(string name)
    => $"(No known documentation for '{name}')\n\nYou can provide one by double clicking or right-clicking the property name or by editing {ComponentDocumentationService.DOC_FILE_NAME}.";

    private static string GetNoDocMessage(string name)
        => $"(No known documentation for '{name}')\n\nYou can provide one by editing {ComponentDocumentationService.DOC_FILE_NAME}.";
}
