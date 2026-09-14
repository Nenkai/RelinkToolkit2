using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;

using Avalonia.Controls;

using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Gma.DataStructures.StringSearch;
using RelinkToolkit2.Services;

namespace RelinkToolkit2.ViewModels.Search;

public partial class ComponentSearchPageViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string? Title { get; set; } = "Search";

    [ObservableProperty]
    public partial ObservableCollection<ComponentSearchItemViewModel> Components { get; set; } = [];

    private readonly Dictionary<string, ComponentSearchItemViewModel> _lut = [];

    public UkkonenTrie<ComponentSearchItemViewModel>? SearchTrie { get; set; }

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
    public ComponentSearchItemViewModel AddComponent(string name, object data, string? icon = "Material.FileCode", string? tag = null, 
        string? hoverText = null, bool showHelpCircle = false, Action<ComponentSearchViewModel, ComponentSearchItemViewModel>? onSelectedCallback = null)
    {
        var component = new ComponentSearchItemViewModel()
        {
            Name = name,
            Data = data,
            Icon = icon,
            Tag = tag,
            HoverText = hoverText,
            InnerRightIcon = showHelpCircle ? "Material.HelpCircle" : null,
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

    public bool TryGetByName(string name, [NotNullWhen(true)] out ComponentSearchItemViewModel? item)
    {
        return _lut.TryGetValue(name, out item);
    }

    public void Components_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
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

    public void SortPage()
    {
        Components = new ObservableCollection<ComponentSearchItemViewModel>(
            Components.OrderByDescending(e => e.IsFolder).ThenBy(e => e.Name));

        foreach (var comp in Components)
        {
            if (comp.Data is ComponentSearchPageViewModel subPage)
                subPage.SortPage();
        }
    }

    public UkkonenTrie<ComponentSearchItemViewModel> BuildSearchTrie()
    {
        UkkonenTrie<ComponentSearchItemViewModel> trie = new();
        BuildSearchTrieForPage(this);

        void BuildSearchTrieForPage(ComponentSearchPageViewModel page)
        {
            foreach (var component in page.Components)
            {
                if (component.Data is ComponentSearchPageViewModel subPage)
                    BuildSearchTrieForPage(subPage);
                else
                    trie.Add(component.Name.ToLower(), component);
            }
        }

        return trie;
    }
}

