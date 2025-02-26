using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GBFRDataTools.FSM;

namespace RelinkToolkit2.ViewModels;

public partial class FSMTreeViewItemViewModel : TreeViewItemViewModel
{
    public required FSMParser FSM { get; set; }

}
