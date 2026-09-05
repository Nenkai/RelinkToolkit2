using CommunityToolkit.Mvvm.ComponentModel;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RelinkToolkit2.Services.Configuration.Models;

public partial class FSMOptions : ObservableObject
{
    [ObservableProperty] public partial bool AlwaysExpandConditions { get; set; }
    [ObservableProperty] public partial bool DisplayFSMGuids { get; set; }
    [ObservableProperty] public partial bool DisplayLayerIndices { get; set; }

    public Dictionary<string, List<string>> RecentComponentTypes { get; set; } = [];
}
