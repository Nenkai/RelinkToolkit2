using Avalonia.Controls;
using Avalonia.Markup.Xaml.Templates;

using CommunityToolkit.Mvvm.Messaging;

using GBFRDataTools.FSM.Components;

using Nodify;

using RelinkToolkit2.Messages.Fsm;
using RelinkToolkit2.ViewModels.Documents;
using RelinkToolkit2.ViewModels.Fsm;
using RelinkToolkit2.ViewModels.Fsm.TransitionComponents;
using RelinkToolkit2.ViewModels.Search;
using RelinkToolkit2.Views.Documents.Fsm;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RelinkToolkit2.Views;

/// <summary>
/// Interaction logic for ConnectorEditorView.xaml
/// </summary>
public partial class ConnectionEditorView : UserControl
{
    public ConnectionEditorView()
    {
        InitializeComponent();
    }

    private void ConnectionExpander_PointerEntered(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (sender is not Control control)
            return;

        var dataContext = control.DataContext;
        if (dataContext is not TransitionViewModel transition || transition.Source == transition.Target)
            return;

        GraphConnectionViewModel connection = transition.ParentConnection;
        bool isBackwards = connection.Target == transition.Source;
        connection.SetAnimatingState(true, isBackwards);
    }

    private void ConnectionExpander_PointerExited(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (sender is not Control control)
            return;

        var dataContext = control.DataContext;
        if (dataContext is not TransitionViewModel transition)
            return;

        GraphConnectionViewModel connection = transition.ParentConnection;
        connection.SetAnimatingState(false, false);
    }

    private void Button_OperandType_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not Control control)
            return;

        var dataContext = control.DataContext;
        if (dataContext is not TransitionConditionOpViewModel condVm)
            return;

        if (condVm.Operand == TransitionOperandType.AND)
            condVm.Operand = TransitionOperandType.OR;
        else
            condVm.Operand = TransitionOperandType.AND;
    }

    private static IEnumerable<Type>? _conditionTypes;
    private void Button_AddConditionComponent(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var flyout = new Flyout();

        var content = new ComponentSearchView();
        flyout.Content = content;

        var searchVM = new ComponentSearchViewModel();
        content.DataContext = searchVM;

        var control = sender as Control;
        searchVM.Context = control!.DataContext as TransitionViewModel;

        if (_conditionTypes is null)
        {
            _conditionTypes = Assembly.GetAssembly(typeof(BehaviorTreeComponent))!.GetTypes()
                .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(ConditionComponent)));
        }

        string baseNamespace = typeof(ConditionComponent).Namespace!;
        ComponentSearchPageViewModel mainPage = new ComponentSearchPageViewModel();
        foreach (Type compType in _conditionTypes)
        {
            if (compType.Namespace is null)
                continue;

            if (compType.Namespace.StartsWith("GBFRDataTools.FSM.Components.Conditions"))
            {
                string desc = compType.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty;

                if (compType.Namespace == "GBFRDataTools.FSM.Components.Conditions")
                {
                    mainPage.AddComponent(compType.Name, compType, desc, OnAddConditionComponent);
                }
                else
                {
                    string subNamespace = compType.Namespace["GBFRDataTools.FSM.Components.Conditions".Length..];
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
                                currentPage.AddComponent(compType.Name, compType, desc, OnAddConditionComponent);
                            else
                                pageName += " > ";
                        }
                    }
                }
            }
        }

        searchVM.AddPage(mainPage);
        searchVM.SortAndBuildSearch();
        flyout.ShowAt(control, showAtPointer: false);
    }

    private void OnAddConditionComponent(ComponentSearchViewModel searchVM, ComponentSearchItemViewModel item)
    {
        if (item.Data is not Type type)
            return;

        if (searchVM.Context is not TransitionViewModel transition)
            return;

        var component = Activator.CreateInstance(type);
        if (component is not ConditionComponent conditionComponent)
            return;

        WeakReferenceMessenger.Default.Send(new TransitionComponentAddRequest() { Transition = transition, Component = conditionComponent });
    }
}
