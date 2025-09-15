using Avalonia.Controls;
using Avalonia.PropertyGrid.Controls;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using Dock.Model.Mvvm.Controls;

using GBFRDataTools.FSM.Components.Actions.Quest;

using ReactiveUI;

using RelinkToolkit2.Messages.Fsm;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RelinkToolkit2.ViewModels;

public partial class PropertyGridViewModel : Tool
{
    [ObservableProperty]
    public object? _selectedObject;

    [ObservableProperty]
    public string? _title;

    [ObservableProperty]
    public string? _description;

    private string _objectTitle;
    private string _objectDescription;

    public PropertyGridViewModel()
    {
        Id = "Properties";
        Title = "Properties";

        WeakReferenceMessenger.Default.Register<FsmComponentSelectedMessage>(this, (recipient, message) =>
        {
            SelectedObject = message.Value;
            UpdateWithComponentName(this.SelectedObject);
        });

        if (Design.IsDesignMode)
        {
            this.SelectedObject = new BeginSection();
            UpdateWithComponentName(this.SelectedObject);
        }
    }

    private void UpdateWithComponentName(object obj)
    {
        var type = obj.GetType();
        this.Title = type.Name;
        this.Description = type.GetCustomAttribute<DescriptionAttribute>()?.Description ?? "(no known documentation)";
        _objectTitle = this.Title;
        _objectDescription = this.Description;
    }

    [RelayCommand]
    public void OnPropertyGotFocus(PropertyGotFocusEventArgs @event)
    {
        this.Title = @event.Context.Property.Name;
        this.Description = !string.IsNullOrEmpty(@event.Context.Property.Description) ? @event.Context.Property.Description : "(no known documentation)";
    }

    [RelayCommand]
    public void OnPropertyLostFocus(PropertyLostFocusEventArgs @event)
    {
        this.Title = _objectTitle;
        this.Description = _objectDescription;
    }

    [RelayCommand]
    public void OnPointerEnteredPropertyName(PropertyCellContext context)
    {
        this.Title = context.Property.Name;
        JsonPropertyNameAttribute? jsonPropNameAttr = context.Property.Attributes.OfType<JsonPropertyNameAttribute>().FirstOrDefault();
        if (jsonPropNameAttr is not null)
            this.Title += $" ({jsonPropNameAttr.Name})";
        
        this.Description = !string.IsNullOrEmpty(context.Property.Description) ? context.Property.Description : "(no known documentation)";
    }

    [RelayCommand]
    public void OnPointerExitedPropertyName(PropertyCellContext context)
    {
        this.Title = _objectTitle;
        this.Description = _objectDescription;
    }
}
