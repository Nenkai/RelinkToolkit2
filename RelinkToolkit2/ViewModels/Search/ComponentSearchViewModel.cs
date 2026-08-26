using Avalonia.Controls;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Gma.DataStructures.StringSearch;

using RelinkToolkit2.Services;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    /// User data context/parameter for the search such as where it was spawned from.
    /// </summary>
    public object? UserData { get; set; }

    /// <summary>
    /// Type of search.
    /// </summary>
    public ComponentSearchCategoryType SearchCategoryType { get; set; }

    internal Action<ComponentSearchViewModel, ComponentSearchItemViewModel>? OnSelected { get; set; }

    // Cached main page, used for restoring main page after searching is done
    private ComponentSearchPageViewModel _mainPage;

    [RelayCommand]
    public void OnSearchTextClear()
    {
        SearchText = string.Empty;
    }

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
            if (_mainPage.SearchTrie is not null)
            {
                List<ComponentSearchItemViewModel> items = _mainPage.SearchTrie.Retrieve(value.ToLower());
                var filterPage = new ComponentSearchPageViewModel();
                if (items is not null)
                {
                    foreach (var item in items.Distinct())
                        filterPage.AddComponent(item);
                }

                SortPage(filterPage);

                Pages[0] = filterPage;
            }
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
            mainPage.AddComponent("Page 0, Entry 2", 0, caption: "lol", tag: "Tag 2.0");
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

    private void SortPage(ComponentSearchPageViewModel page)
    {
        page.Components = new ObservableCollection<ComponentSearchItemViewModel>(
            page.Components.OrderByDescending(e => e.IsFolder).ThenBy(e => e.Name));

        foreach (var comp in page.Components)
        {
            if (comp.Data is ComponentSearchPageViewModel subPage)
                SortPage(subPage);
        }
    }
}
