using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using GBFRDataTools.FSM.Entities;

using RelinkToolkit2.Messages.Fsm;

namespace RelinkToolkit2.ViewModels.Fsm.TransitionComponents;

public partial class TransitionConditionBase : ObservableObject
{
    [ObservableProperty]
    private string? _title;
}
