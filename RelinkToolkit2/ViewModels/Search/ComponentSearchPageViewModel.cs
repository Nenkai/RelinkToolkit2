using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;

using Avalonia.Controls;

using CommunityToolkit.Mvvm.ComponentModel;

namespace RelinkToolkit2.ViewModels.Search;

public partial class ComponentSearchPageViewModel : ObservableObject
{
    [ObservableProperty]
    private string? _title = "Search";

    [ObservableProperty]
    private ObservableCollection<ComponentSearchItemViewModel> _components = [];

    private Dictionary<string, ComponentSearchItemViewModel> _lut = [];

    public ComponentSearchPageViewModel()
    {
        if (Design.IsDesignMode)
        {
            Components.Add(new ComponentSearchItemViewModel() { Name = "Hello" });
            Components.Add(new ComponentSearchItemViewModel() { Name = "World" });
        }

        Components.CollectionChanged += Components_CollectionChanged;
    }

    /// <summary>
    /// Adds a new item to the page.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="data"></param>
    /// <param name="caption"></param>
    public ComponentSearchItemViewModel AddComponent(string name, object data, string? caption = null, Action<ComponentSearchViewModel, ComponentSearchItemViewModel>? onSelectedCallback = null)
    {
        var component = new ComponentSearchItemViewModel()
        {
            Name = name,
            Data = data,
            HelpCaption = caption,
            InnerRightIcon = !string.IsNullOrEmpty(caption) ? "Material.HelpCircle" : null,
            OnSelected = onSelectedCallback,
        };

        Components.Add(component);
        return component;
    }

    /// <summary>
    /// Adds a new sub-page to the page.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="data"></param>
    /// <param name="caption"></param>
    public ComponentSearchItemViewModel AddPageEntry(string name, ComponentSearchPageViewModel page)
    {
        var component = new ComponentSearchItemViewModel()
        {
            Name = name,
            Data = page,
        };

        Components.Add(component);
        return component;
    }

    public void AddComponent(ComponentSearchItemViewModel item)
    {
        Components.Add(item);
    }

    public bool TryGetByName(string name, out ComponentSearchItemViewModel item)
    {
        return _lut.TryGetValue(name, out item);
    }

    public void Components_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            var item = (ComponentSearchItemViewModel)e.NewItems![0]!;
            _lut.TryAdd(item.Name, item);
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove)
        {
            var item = (ComponentSearchItemViewModel)e.NewItems![0]!;
            _lut.Remove(item.Name);
        }
    }
}

