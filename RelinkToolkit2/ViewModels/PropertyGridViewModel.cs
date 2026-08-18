using Avalonia.Controls;
using Avalonia.PropertyGrid.Controls;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using Dock.Model.Mvvm.Controls;

using DynamicData.Aggregation;

using GBFRDataTools.FSM.Components.Actions.Quest;

using ReactiveUI;

using RelinkToolkit2.Messages.Fsm;
using RelinkToolkit2.Services;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RelinkToolkit2.ViewModels;

public partial class PropertyGridViewModel : Tool, 
    IRecipient<FsmComponentSelectedMessage>
{
    private readonly ComponentDocumentationService _componentDocService;

    [ObservableProperty]
    public object? _selectedObject;

    [ObservableProperty]
    public string? _objectName;

    [ObservableProperty]
    public string? _description;

    private string _objectTitle;
    private string _objectDescription;

    public PropertyGridViewModel(ComponentDocumentationService componentDocumentationService)
    {
        _componentDocService = componentDocumentationService;

        Id = "Properties";
        Title = "Properties Viewer";

        if (Design.IsDesignMode)
        {
            SelectedObject = new BeginSection();
            UpdateWithComponentName(SelectedObject);
        }

        WeakReferenceMessenger.Default.RegisterAll(this);
    }

    /// <summary>
    /// Fired when a fsm component is received.
    /// </summary>
    /// <param name="message"></param>
    public void Receive(FsmComponentSelectedMessage message)
    {
        SelectedObject = message.Value;
        UpdateWithComponentName(SelectedObject);
    }

    private void UpdateWithComponentName(object obj)
    {
        var type = obj.GetType();
        ObjectName = type.Name;

        string key = _componentDocService.BuildKey(type);
        string? desc = _componentDocService.GetComponentString(key);
        Description = desc ?? GetNoDocMessage(type.Name);
        _objectTitle = ObjectName;
        _objectDescription = Description;
    }

    [RelayCommand]
    public void OnPropertyGotFocus(PropertyGotFocusEventArgs @event)
    {
        string key = _componentDocService.BuildKey(@event.Context.Property.ComponentType);
        string? desc = _componentDocService.GetPropertyString(key);

        ObjectName = @event.Context.Property.Name;
        Description = !string.IsNullOrWhiteSpace(desc) ? desc : GetNoDocMessage(@event.Context.Property.Name);
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

        string key = _componentDocService.BuildKey(context.Property.ComponentType);
        string? desc = _componentDocService.GetPropertyString(key);

        Description = !string.IsNullOrWhiteSpace(desc) ? desc : GetNoDocMessage(context.Property.Name);
    }


    [RelayCommand]
    public void OnPointerExitedPropertyName(PropertyCellContext context)
    {
        ObjectName = _objectTitle;
        Description = _objectDescription;
    }

    private static string GetNoDocMessage(string name)
        => $"(No known documentation for '{name}')\nYou can provide one by editing {ComponentDocumentationService.DOC_FILE_NAME}.";
}
