using GBFRDataTools.Entities;
using GBFRDataTools.FSM.Components;

using RelinkToolkit2.ViewModels.Documents.GraphEditor.Nodes;
using RelinkToolkit2.ViewModels.Search;
using RelinkToolkit2.Views;
using RelinkToolkit2.Views.Documents.GraphEditor;

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
    private static IEnumerable<Type> _actionComponentTypes = Assembly.GetAssembly(typeof(BehaviorTreeComponent))!.GetTypes()
        .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(ActionComponent)));

    private static IEnumerable<Type> _conditionComponentTypes = Assembly.GetAssembly(typeof(BehaviorTreeComponent))!.GetTypes()
        .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(ConditionComponent)));

    public ComponentSearchViewModel BuildSearchViewModel(object context, ComponentSearchCategoryType componentType,
        Action<ComponentSearchViewModel, ComponentSearchItemViewModel> onSelected)
    {
        var searchVM = new ComponentSearchViewModel();
        searchVM.Context = context;

        string baseNamespace = typeof(ActionComponent).Namespace!;
        ComponentSearchPageViewModel mainPage = new ComponentSearchPageViewModel();

        string typeNamespace = componentType == ComponentSearchCategoryType.Action ? "GBFRDataTools.FSM.Components.Actions" : "GBFRDataTools.FSM.Components.Conditions";
        IEnumerable<Type> types = componentType == ComponentSearchCategoryType.Action ? _actionComponentTypes : _conditionComponentTypes;

        foreach (Type compType in types)
        {
            if (compType.Namespace is null)
                continue;

            if (compType.Namespace.StartsWith(typeNamespace))
            {
                string desc = compType.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty;
                string tag = compType.GetCustomAttribute<GameSupportAttribute>()?.Text ?? string.Empty;

                if (compType.Namespace == typeNamespace)
                {
                    mainPage.AddComponent(compType.Name, compType, desc, tag, onSelected);
                }
                else
                {
                    string subNamespace = compType.Namespace[typeNamespace.Length..];
                    if (!string.IsNullOrEmpty(subNamespace))
                    {
                        string[] subSplit = subNamespace.Substring(1).Split('.');
                        var currentPage = mainPage;

                        string pageName = "";
                        for (int i = 0; i < subSplit.Length; i++)
                        {
                            string? folder = subSplit[i];
                            pageName += folder;

                            if (!currentPage.TryGetByName(folder, out ComponentSearchItemViewModel component))
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
                                currentPage.AddComponent(compType.Name, compType, desc, tag, onSelected);
                            else
                                pageName += " > ";
                        }
                    }
                }
            }
        }

        searchVM.AddPage(mainPage);
        searchVM.SortAndBuildSearch();
        return searchVM;
    }
}

public enum ComponentSearchCategoryType
{
    Action,
    Condition
}
