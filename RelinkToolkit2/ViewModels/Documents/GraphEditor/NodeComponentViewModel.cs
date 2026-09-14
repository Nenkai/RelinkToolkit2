
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using Avalonia.Media;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using GBFRDataTools.Entities;
using GBFRDataTools.FSM;
using GBFRDataTools.FSM.Components;
using GBFRDataTools.FSM.Components.Conditions;

using SmartFormat;

using Microsoft.Extensions.DependencyInjection;

using RelinkToolkit2.Services;
using RelinkToolkit2.ViewModels.Documents.GraphEditor.Nodes;


namespace RelinkToolkit2.ViewModels.Documents.GraphEditor;

/// <summary>
/// Execution component within a node.
/// </summary>
public partial class NodeComponentViewModel : ObservableObject
{
    [ObservableProperty]
    private string? _name;

    [ObservableProperty]
    private string? _caption;

    [ObservableProperty]
    private IBrush _borderBrush = GraphColors.ComponentBorderNormal;

    public BehaviorTreeComponent Component { get; set; }

    [JsonIgnore]
    public NodeViewModel Parent { get; private set; }

    public NodeComponentViewModel(NodeViewModel parent, BehaviorTreeComponent component)
    {
        Parent = parent;
        Component = component;
        Component.PropertyChanged += Component_PropertyChanged;
        UpdateCaption();
    }

    public NodeComponentViewModel CloneAsNewComponent(NodeViewModel parentNode)
    {
        var type = FSMParser.ComponentNameToType[Component.ComponentName];
        var serialized = JsonSerializer.Serialize(Component, type, DefaultJsonSerializerOptions.InstanceForWrite);
        BehaviorTreeComponent? clonedComponent = (BehaviorTreeComponent?)JsonSerializer.Deserialize(serialized, type, DefaultJsonSerializerOptions.InstanceForRead);
        if (clonedComponent is null)
            throw new ArgumentNullException(nameof(clonedComponent));

        var editor = (NodeEditorViewModelBase)Parent.ParentEditor!;
        clonedComponent.ParentGuid = parentNode.Guid;
        clonedComponent.Guid = editor.GetNewGuid();
        return new NodeComponentViewModel(parentNode, clonedComponent)
        {
            Name = Name,
            Caption = Caption,
        };
    }
    public void SetParentNode(NodeViewModel parentNode)
    {
        Parent = parentNode;
        Component.ParentGuid = parentNode.Guid;
    }

    private void Component_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        UpdateCaption();
    }

    public void UpdateCaption()
    {
        var docService = App.Current.Services.GetRequiredService<ComponentDocumentationService>();
        string? formatString = docService.GetComponentPreviewFormatString(Component.GetType());
        string? caption = null;
        if (!string.IsNullOrWhiteSpace(formatString))
        {
            try
            {
                caption = Smart.Format(formatString, Component);
            }
            catch (Exception)
            {
                caption = $"<Caption Formatting Error>";
            }
        }
        else
        {
            caption = Component.GetCaption();
        }

        if (Component is ConditionComponent conditionComponent)
        {
            if (!string.IsNullOrWhiteSpace(caption))
                caption += " ";

            caption += $"is {!conditionComponent.IsReverseSuccess}";
        }

        Caption = caption;
    }

    [RelayCommand]
    public void OnComponentDelete(object param)
    {
        ;
    }
}
