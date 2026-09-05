using System.Text.Json.Serialization;

using CommunityToolkit.Mvvm.ComponentModel;

namespace RelinkToolkit2.ViewModels.Documents;

public partial class GenericEntityEntryViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string? Title { get; set; }

    public required object Object { get; set; }
}
