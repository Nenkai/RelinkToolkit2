using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

using CommunityToolkit.Mvvm.ComponentModel;

namespace RelinkToolkit2.ViewModels.Documents.GraphEditor.TransitionComponents;

/// <summary>
/// Represents a condition element, that can be either an operator group, or an actual condition
/// </summary>
public partial class TransitionConditionElementViewModelBase : ObservableObject
{
    /// <summary>
    /// Parent operand where this element is contained
    /// </summary>
    public TransitionConditionOpViewModel? ParentOperand { get; set; }
}
