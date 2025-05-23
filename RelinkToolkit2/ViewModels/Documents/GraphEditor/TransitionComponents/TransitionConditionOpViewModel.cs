﻿using System;
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

namespace RelinkToolkit2.ViewModels.Documents.GraphEditor.TransitionComponents;

/// <summary>
/// Represents a transition between two nodes on the graph.
/// </summary>
public partial class TransitionConditionOpViewModel : TransitionConditionBase
{
    [ObservableProperty]
    private int _priority;

    [ObservableProperty]
    private TransitionOperandType? _operand;

    public TransitionConditionOpViewModel()
    {
        Operand = TransitionOperandType.AND;
    }

    partial void OnOperandChanged(TransitionOperandType? value)
    {
        Title = value.ToString();
    }
}

public enum TransitionOperandType
{
    AND,
    OR,
}
