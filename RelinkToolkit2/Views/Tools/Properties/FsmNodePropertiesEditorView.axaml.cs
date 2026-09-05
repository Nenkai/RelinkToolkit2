using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using CommunityToolkit.Mvvm.Messaging;

using GBFRDataTools.FSM.Components;
using GBFRDataTools.FSM.Components.Actions;
using GBFRDataTools.FSM.Components.Conditions;

using Microsoft.Extensions.DependencyInjection;

using RelinkToolkit2.Messages.Fsm;
using RelinkToolkit2.Services;
using RelinkToolkit2.ViewModels.Documents.GraphEditor;
using RelinkToolkit2.ViewModels.Documents.GraphEditor.Nodes;
using RelinkToolkit2.ViewModels.Search;
using RelinkToolkit2.ViewModels.Tools.Properties;
using RelinkToolkit2.Views.Search;

using System;

namespace RelinkToolkit2.Views.Tools.Properties;

public partial class FsmNodePropertiesEditorView : UserControl
{
    public FsmNodePropertiesEditorView()
    {
        InitializeComponent();
    }

    private void Button_AddComponent(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var control = sender as Control;
        var properties = (FsmNodePropertiesEditorViewModel)control!.DataContext!;

        if (!properties.Node!.CanHaveComponents)
            return;

        ComponentSearchService componentSearchService = App.Current?.Services!.GetRequiredService<ComponentSearchService>()!;
        ComponentSearchViewModel searchVM = componentSearchService.BuildSearchViewModel(properties.Node,
            ComponentSearchCategoryType.Action, onSelected: OnAddComponent);

        var content = new ComponentSearchView { DataContext = searchVM };
        var flyout = new Flyout
        {
            Content = content
        };
        flyout.ShowAt(control, showAtPointer: false);
    }

    private void OnAddComponent(ComponentSearchViewModel searchVM, ComponentSearchItemViewModel item)
    {
        if (item.Data is not Type type)
            return;

        if (searchVM.UserData is not FsmNodeViewModel nodeViewModel)
            return;

        var component = Activator.CreateInstance(type);
        if (component is not BehaviorTreeComponent btComponent)
            return;

        var fsmEditor = (FsmEditorViewModel)nodeViewModel.ParentEditor;
        btComponent.ParentGuid = nodeViewModel.Guid;
        btComponent.Guid = fsmEditor.GetNewGuid();
        if (btComponent is QuestActionComponent questActionComponent)
        {
            if (fsmEditor.HasQuestContext())
            {
                QuestContext questContext = fsmEditor.GetQuestContext()!;
                questActionComponent.Category = questContext.Category;
                questActionComponent.Subcategory = questContext.SubCategory;
                questActionComponent.Index = questContext.Index;
                questActionComponent.ProgressIndex = questContext.ProgressIndex;
                questActionComponent.ProgressHash = questContext.ProgressHash;
            }
        }
        else if (btComponent is QuestConditionComponent questConditionComponent)
        {
            if (fsmEditor.HasQuestContext())
            {
                QuestContext questContext = fsmEditor.GetQuestContext()!;
                questConditionComponent.Category = questContext.Category;
                questConditionComponent.Subcategory = questContext.SubCategory;
                questConditionComponent.Index = questContext.Index;
                questConditionComponent.ProgressIndex = questContext.ProgressIndex;
                questConditionComponent.ProgressHash = questContext.ProgressHash;
            }
        }

        fsmEditor.RegisterFsmElementGuid(btComponent.Guid, btComponent);
        nodeViewModel.AddComponent(btComponent);

        // Make sure to select it.
        WeakReferenceMessenger.Default.Send(new FsmComponentSelectedMessage(btComponent));
    }
}