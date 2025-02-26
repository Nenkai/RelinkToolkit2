using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Media;

using CommunityToolkit.Mvvm.ComponentModel;

namespace RelinkToolkit2.ViewModels.Fsm;

public partial class NodeViewModelBase : ObservableObject
{
    [ObservableProperty]
    private Point _location;

    [ObservableProperty]
    public string? _title;
}
