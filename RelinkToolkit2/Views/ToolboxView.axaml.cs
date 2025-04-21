using Aldwych.Logging;

using Avalonia.Controls;
using Avalonia.Markup.Xaml.Templates;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace RelinkToolkit2.Views;

/// <summary>
/// Interaction logic for ToolboxView.xaml
/// </summary>
public partial class ToolboxView : UserControl
{
    private readonly ILogger logger;

    public ToolboxView()
    {
        InitializeComponent();
    }
}
