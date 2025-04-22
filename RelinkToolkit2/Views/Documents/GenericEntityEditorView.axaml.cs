using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;

using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using GBFRDataTools.FSM.Components;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;

using Microsoft.Msagl.Layout.Layered;

using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

using Nodify;

using RelinkToolkit2.Messages.Fsm;
using RelinkToolkit2.Services;
using RelinkToolkit2.ViewModels;
using RelinkToolkit2.ViewModels.Documents;
using RelinkToolkit2.ViewModels.Fsm;
using RelinkToolkit2.ViewModels.Fsm.TransitionComponents;
using RelinkToolkit2.ViewModels.Menu;
using RelinkToolkit2.ViewModels.Search;
using RelinkToolkit2.Views.Documents.Fsm;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RelinkToolkit2.Views.Documents;

public partial class GenericEntityEditorView : UserControl
{
    public GenericEntityEditorView()
    {
        InitializeComponent();
    }

    private void RegisterMessages()
    {
        
    }
}