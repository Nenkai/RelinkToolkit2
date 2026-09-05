using CommunityToolkit.Mvvm.ComponentModel;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RelinkToolkit2.ViewModels.TreeView;

public partial class ComponentTreeViewItemViewModel : TreeViewItemViewModel
{
    public Type ComponentType { get; set; }

    public ComponentTreeViewItemViewModel(Type type)
    {
        ComponentType = type;
    }
}
