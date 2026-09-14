using System;

using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

using ColorTextBlock.Avalonia;

using CommunityToolkit.Mvvm.Messaging;

using GBFRDataTools.FSM.Components.Conditions;

using Microsoft.Extensions.DependencyInjection;

using RelinkToolkit2.Services;
using RelinkToolkit2.ViewModels.Documents.GraphEditor;
using RelinkToolkit2.ViewModels.Documents.GraphEditor.TransitionComponents;
using RelinkToolkit2.ViewModels.Search;
using RelinkToolkit2.Views.Search;

namespace RelinkToolkit2.Views.Tools;

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

        FsmConnectionViewModel connection = transition.ParentConnection;
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

        FsmConnectionViewModel connection = transition.ParentConnection;
        connection.SetAnimatingState(false, false);
    }

    private void Button_AddConditionComponent(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var control = sender as Control;

        ComponentSearchService componentSearchService = App.Current?.Services!.GetRequiredService<ComponentSearchService>()!;
        ComponentSearchViewModel searchVM = componentSearchService.BuildSearchViewModel((TransitionConditionOpViewModel)control!.DataContext!,
            ComponentSearchCategoryType.Condition, onSelected: OnAddConditionComponent);

        var content = new ComponentSearchView { DataContext = searchVM };
        var flyout = new Flyout
        {
            Content = content
        };
        flyout.ShowAt(control, showAtPointer: false);
    }

    private void OnAddConditionComponent(ComponentSearchViewModel searchVM, ComponentSearchItemViewModel item)
    {
        if (item.Data is not Type type)
            return;

        if (searchVM.UserData is not TransitionConditionOpViewModel operatorGroup)
            return;

        var component = Activator.CreateInstance(type);
        if (component is not ConditionComponent conditionComponent)
            return;

        WeakReferenceMessenger.Default.Send(new TransitionComponentAddRequest()
        {
            OperatorGroup = operatorGroup,
            Component = conditionComponent
        });
    }

    private void ConditionComponentTitle_OnToolTipOpening(object? sender, Avalonia.Interactivity.CancelRoutedEventArgs e)
    {
        if (sender is not Control control)
            return;

        var transitionCondition = (TransitionConditionViewModel)control.DataContext!;

        ComponentDocumentationService docService = App.Current.Services.GetRequiredService<ComponentDocumentationService>();

        string tip = $"#### {transitionCondition.ConditionComponent.ComponentName}\n\n";
        string? docString = docService.GetComponentString(transitionCondition.ConditionComponent.GetType());
        if (!string.IsNullOrWhiteSpace(docString))
            tip += docString;
        else
            tip += $"**No documentation was found for component **`{transitionCondition.ConditionComponent.ComponentName}`.";

        var stackPanel = new StackPanel();
        var markdown = new Markdown.Avalonia.MarkdownScrollViewer()
        {
            Markdown = tip
        };

        var style = new Style(x => x.OfType<CTextBlock>());
        style.Setters.Add(new Setter(CTextBlock.FontFamilyProperty, new FontFamily("Segoe UI")));
        markdown.Styles.Add(style);

        stackPanel.Children.Add(markdown);

        ToolTip.SetTip(control, stackPanel);
    }
}
