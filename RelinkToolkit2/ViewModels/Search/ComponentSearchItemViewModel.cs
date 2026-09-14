using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;

namespace RelinkToolkit2.ViewModels.Search;

public partial class ComponentSearchItemViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Name { get; set; } = "Item";

    /// <summary>
    /// Lefthand side icon
    /// </summary>
    [ObservableProperty]
    public partial string? Icon { get; set; }

    /// <summary>
    /// Righthand side icon
    /// </summary>
    [ObservableProperty]
    public partial string? InnerRightIcon { get; set; }

    /// <summary>
    /// User-data associated to this item.
    /// </summary>
    [ObservableProperty]
    public partial object? Data { get; set; }

    /// <summary>
    /// Display tag for the item
    /// </summary>
    [ObservableProperty]
    public partial string? Tag { get; set; }

    /// <summary>
    /// Text to display upon hovering this item
    /// </summary>
    [ObservableProperty]
    public partial string? HoverText { get; set; }

    /// <summary>
    /// Whether this item leads to another page - it is a folder
    /// </summary>
    public bool IsFolder => Data is ComponentSearchPageViewModel;

    public Action<ComponentSearchViewModel, ComponentSearchItemViewModel>? OnSelected { get; set; }

    public void SetPage(ComponentSearchPageViewModel page)
    {
        Data = page;
    }

    partial void OnDataChanged(object? value)
    {
        // If the data for this item is a page, set an icon
        if (value is ComponentSearchPageViewModel)
        {
            InnerRightIcon = "Material.ChevronRight";
        }
    }

    public override string ToString()
    {
        string str = Name;
        if (IsFolder)
            str += " (Folder)";
        return str;
    }
}
