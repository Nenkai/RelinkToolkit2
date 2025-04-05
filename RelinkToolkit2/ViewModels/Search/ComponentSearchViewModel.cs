using Avalonia.Controls;

using CommunityToolkit.Mvvm.ComponentModel;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Gma.DataStructures.StringSearch;

namespace RelinkToolkit2.ViewModels.Search;

public partial class ComponentSearchViewModel : ObservableObject
{
    [ObservableProperty]
    private int _pageIndex;

    /// <summary>
    /// Current pages in the search.
    /// </summary>
    public ObservableCollection<ComponentSearchPageViewModel> Pages { get; } = [];

    /// <summary>
    /// Search bar text.
    /// </summary>
    [ObservableProperty]
    private string? _searchText;

    [ObservableProperty]
    private bool? _isOpen;

    /// <summary>
    /// Data context/parameter for the search.
    /// </summary>
    public object? Context { get; set; }

    // Used for searching
    private readonly UkkonenTrie<ComponentSearchItemViewModel> _trie = new();

    // Cached main page, used for restoring main page after searching is done
    private ComponentSearchPageViewModel _mainPage;

    partial void OnIsOpenChanged(bool? value)
    {
        throw new NotImplementedException();
    }

    partial void OnSearchTextChanged(string? value)
    {
        if (Pages.Count == 0)
            return;

        PageIndex = 0;

        if (string.IsNullOrEmpty(value))
        {
            Pages[0] = _mainPage;
        }
        else
        {
            List<ComponentSearchItemViewModel> items = _trie.Retrieve(value.ToLower());
            var filterPage = new ComponentSearchPageViewModel();
            if (items is not null)
            {
                foreach (var item in items)
                    filterPage.AddComponent(item);
            }

            SortPage(filterPage);

            Pages[0] = filterPage;
        }
    }

    public ComponentSearchViewModel()
    {
        if (Design.IsDesignMode)
        {
            var mainPage = new ComponentSearchPageViewModel();

            var subPage = new ComponentSearchPageViewModel();
            subPage.AddComponent("Sub-Entry", 0);
            mainPage.AddPageEntry("Page 0, Entry 0 (folder)", subPage);
            mainPage.AddComponent("Page 0, Entry 1", 0);
            mainPage.AddComponent("Page 0, Entry 2", 0, caption: "lol");
            AddPage(mainPage);
        }
    }

    public void AddPage(ComponentSearchPageViewModel page)
    {
        Pages.Add(page);
        if (Pages.Count == 1)
        {
            PageIndex = 0;
            _mainPage = page;
        }
    }

    public void SortAndBuildSearch()
    {
        if (Pages.Count == 0)
            return;

        SortPage(Pages[0]);
    }

    private void SortPage(ComponentSearchPageViewModel page)
    {
        // NOTE: Can probably be optimized.
        page.Components.CollectionChanged -= page.Components_CollectionChanged;
        page.Components = new ObservableCollection<ComponentSearchItemViewModel>(page.Components.OrderByDescending(e => e.IsFolder).ThenBy(e => e.Name));
        page.Components.CollectionChanged += page.Components_CollectionChanged;

        foreach (var comp in page.Components)
        {
            if (comp.Data is ComponentSearchPageViewModel subPage)
                SortPage(subPage);
            else
                _trie.Add(comp.Name.ToLower(), comp);
        }
    }
}
