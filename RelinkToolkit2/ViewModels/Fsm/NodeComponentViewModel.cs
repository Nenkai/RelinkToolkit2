using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Avalonia.Media;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using GBFRDataTools.FSM.Entities;

namespace RelinkToolkit2.ViewModels.Fsm;

public partial class NodeComponentViewModel : ObservableObject
{
    [ObservableProperty]
    private string? _name;

    [ObservableProperty]
    private IBrush _borderBrush = GraphColors.ComponentBorderNormal;

    public required BehaviorTreeComponent Component { get; set; }

    public NodeViewModel Parent { get; set; }

    public NodeComponentViewModel(NodeViewModel parent)
    {
        Parent = parent;
    }

    [RelayCommand]
    public void OnComponentDelete(object param)
    {
        ;
    }
}
