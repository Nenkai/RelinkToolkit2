using Avalonia.Controls;

using GBFRDataTools.Entities;
using GBFRDataTools.FSM.Components;
using GBFRDataTools.FSM.Components.Actions;
using GBFRDataTools.FSM.Components.Conditions;

using Gma.DataStructures.StringSearch;

using RelinkToolkit2.Collections;
using RelinkToolkit2.ViewModels.Search;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace RelinkToolkit2.Services;

public class ComponentSearchService
{
    private readonly ComponentDocumentationService _componentDocumentation;

    public Dictionary<ComponentSearchCategoryType, ComponentSearchPageCache> _caches = [];

    public ComponentSearchService(ComponentDocumentationService componentDocumentation)
    {
        _componentDocumentation = componentDocumentation;
    }

    /// <summary>
    /// Builds a component search
    /// </summary>
    /// <param name="context">Context (usually a view model)</param>
    /// <param name="searchCategoryType">Type of component (action or condition)</param>
    /// <param name="onSelected">Callback on item selected</param>
    /// <returns></returns>
    public ComponentSearchViewModel BuildSearchViewModel(object context, ComponentSearchCategoryType searchCategoryType,
        Action<ComponentSearchViewModel, ComponentSearchItemViewModel> onSelected)
    {
        if (!_caches.TryGetValue(searchCategoryType, out ComponentSearchPageCache? mainPageCache))
        {
            Type type = searchCategoryType == ComponentSearchCategoryType.Action ? typeof(ActionComponent) : typeof(ConditionComponent);

            mainPageCache = CreateForComponentType(type);
            _caches[searchCategoryType] = mainPageCache;

            mainPageCache.Root.SortPage();
        }

        // Clone it (different context)
        var mainPage = new ComponentSearchPageViewModel()
        {
            Title = mainPageCache.Root.Title,
            SearchTrie = mainPageCache.Root.SearchTrie,
        };
        foreach (var component in mainPageCache.Root.Components)
            mainPage.AddComponent(component);

        var searchVM = new ComponentSearchViewModel
        {
            UserData = context,
            OnSelected = onSelected,
            SearchCategoryType = searchCategoryType,
        };
        searchVM.AddPage(mainPage);

        // Recent items at the top
        if (mainPageCache.RecentItems.Count != 0)
        {
            var page = new ComponentSearchPageViewModel()
            {
                Title = "Recent Items"
            };

            foreach (var item in mainPageCache.RecentItems)
            {
                page.AddComponent(item);
            }

            mainPage.Components.Insert(0, new ComponentSearchItemViewModel()
            {
                Name = "Recent Items...",
                Icon = "Material.History",
                Data = page,
            });
        }

        return searchVM;
    }

    private static IEnumerable<Type> GetAllTypesSubclassing(Type type) => 
        Assembly.GetAssembly(typeof(BehaviorTreeComponent))!
            .GetTypes()
            .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(type));

    private ComponentSearchPageCache CreateForComponentType(Type type)
    {
        IEnumerable<Type> inheritingTypes = GetAllTypesSubclassing(type);
        string typeNamespace = type.Namespace!;

        var mainPage = new ComponentSearchPageViewModel();
        foreach (Type compType in inheritingTypes)
        {
            if (compType.Namespace is null)
                continue;

            if (compType.Namespace.StartsWith(typeNamespace))
            {
                string key = _componentDocumentation.BuildKey(compType);
                string desc = _componentDocumentation.GetComponentString(key) ?? string.Empty;
                string tag = compType.GetCustomAttribute<GameSupportAttribute>()?.Text ?? string.Empty;
                string hoverText = GetHoverText(compType);

                if (compType.Namespace == typeNamespace)
                {
                    mainPage.AddComponent(compType.Name, compType, desc, tag: tag, hoverText: hoverText, onSelectedCallback: OnSelected);
                }
                else
                {
                    string subNamespace = compType.Namespace[typeNamespace.Length..];
                    if (!string.IsNullOrEmpty(subNamespace))
                    {
                        string[] subSplit = subNamespace.Substring(1).Split('.');
                        ComponentSearchPageViewModel currentPage = mainPage;

                        string pageName = "";
                        for (int i = 0; i < subSplit.Length; i++)
                        {
                            string? folder = subSplit[i];
                            pageName += folder;

                            if (!currentPage.TryGetByName(folder, out ComponentSearchItemViewModel? component))
                            {
                                component = new ComponentSearchItemViewModel() { Name = folder };
                                currentPage.AddComponent(component);

                                ComponentSearchPageViewModel subPage = new()
                                {
                                    Title = pageName,
                                };

                                component.SetPage(subPage);
                                currentPage = subPage;
                            }
                            else
                            {
                                currentPage = ((ComponentSearchPageViewModel)component.Data!);
                            }

                            if (i == subSplit.Length - 1)
                                currentPage.AddComponent(compType.Name, compType, desc, tag: tag, hoverText: hoverText, onSelectedCallback: OnSelected);
                            else
                                pageName += " > ";
                        }
                    }
                }
            }
        }
        mainPage.SortPage();

        UkkonenTrie<ComponentSearchItemViewModel> trie = mainPage.BuildSearchTrie();
        mainPage.SearchTrie = trie;

        return new ComponentSearchPageCache()
        {
            Root = mainPage,
        };
    }

    private static string GetHoverText(Type compType)
    {
        List<Type> baseTypes = [];
        for (Type? baseType = compType; baseType?.BaseType != null && baseType != typeof(BehaviorTreeComponent); baseType = baseType.BaseType)
        {
            baseTypes.Add(baseType);
        }

        string hoverText = $"Inheritance tree:\n";
        for (int i = 0; i < baseTypes.Count; i++)
        {
            hoverText += baseTypes[i].Name;
            if (i != baseTypes.Count - 1)
                hoverText += "\n" + new string(' ', 2) + "-> ";
        }

        return hoverText;
    }

    public void OnSelected(ComponentSearchViewModel searchVM, ComponentSearchItemViewModel itemVM)
    {
        _caches[searchVM.SearchCategoryType].RecentItems.Push(itemVM);

        searchVM?.OnSelected?.Invoke(searchVM, itemVM);
    }

    public class ComponentSearchPageCache
    {
        public required ComponentSearchPageViewModel Root { get; set; }
        public RecentItemsStack<ComponentSearchItemViewModel> RecentItems { get; set; } = new(20);
    }
}

public enum ComponentSearchCategoryType
{
    Action,
    Condition
}
