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
    private string? _name;

    [ObservableProperty]
    private string? _icon = "Material.FileCode";

    [ObservableProperty]
    private string? _innerRightIcon;

    [ObservableProperty]
    private string? _helpCaption;

    [ObservableProperty]
    private object? _data;

    public Action<ComponentSearchViewModel, ComponentSearchItemViewModel>? OnSelected { get; set; }

    public bool IsFolder => Data is ComponentSearchPageViewModel;

    public ComponentSearchItemViewModel()
    {

    }

    public void SetPage(ComponentSearchPageViewModel page)
    {
        Data = page;
    }

    partial void OnDataChanged(object? value)
    {
        if (value is ComponentSearchPageViewModel)
        {
            Icon = string.Empty;
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
