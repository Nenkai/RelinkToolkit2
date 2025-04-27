using Avalonia.Media;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using GBFRDataTools.FSM.Components;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

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

    public NodeViewModel Parent { get; set; }

    public NodeComponentViewModel(NodeViewModel parent, BehaviorTreeComponent component)
    {
        Parent = parent;
        Component = component;
        Component.PropertyChanged += Component_PropertyChanged;
        UpdateCaption(component);
    }

    private void Component_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        UpdateCaption(Component);
    }

    private void UpdateCaption(BehaviorTreeComponent btComponent)
    {
        Caption = btComponent.GetCaption();
    }

    public static string GetEnumDescription1(Enum value)
    {
        if (value == null) { return ""; }

        var type = value.GetType();
        var field = type.GetField(value.ToString());
        var custAttr = field?.GetCustomAttributes(typeof(DescriptionAttribute), false);
        DescriptionAttribute attribute = custAttr?.SingleOrDefault() as DescriptionAttribute;
        return attribute == null ? value.ToString() : attribute.Description;
    }

    [RelayCommand]
    public void OnComponentDelete(object param)
    {
        ;
    }
}
