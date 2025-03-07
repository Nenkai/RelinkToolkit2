﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Avalonia.Controls;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

using Dock.Model.Mvvm.Controls;

using GBFRDataTools.FSM.Components.Actions.Quest;

using RelinkToolkit2.Messages.Fsm;

namespace RelinkToolkit2.ViewModels;

public partial class PropertyGridViewModel : Tool
{
    [ObservableProperty]
    public object? _selectedObject;

    public PropertyGridViewModel()
    {
        Id = "Properties";
        Title = "Properties";

        WeakReferenceMessenger.Default.Register<FsmComponentSelectedMessage>(this, (recipient, message) =>
        {
            SelectedObject = message.Value;
        });

        if (Design.IsDesignMode)
        {
            this.SelectedObject = new BeginSection();
        }
    }
}
