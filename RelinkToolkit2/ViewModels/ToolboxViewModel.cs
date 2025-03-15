using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Dock.Model.Mvvm.Controls;

using GBFRDataTools.FSM.Entities;

using RelinkToolkit2.ViewModels.Documents;

//using MsBox.Avalonia;

namespace RelinkToolkit2.ViewModels;

public partial class ToolboxViewModel : Tool //, IDragSource
{
    private readonly Dictionary<string, TreeViewItemViewModel> _idToItem = [];
    public ObservableCollection<TreeViewItemViewModel> DisplayedItems { get; set; } = [];

    public ToolboxViewModel() 
    {
        Id = "Toolbox";
        Title = "Toolbox";

        var componentTvm = new TreeViewItemViewModel()
        {
            TreeViewName = "Components",
            IsExpanded = true,
            IconKind = "Material.Chip",
        };

        AddItem("components", componentTvm);

        IEnumerable<Type> componentTypes = Assembly.GetAssembly(typeof(BehaviorTreeComponent))!.GetTypes()
            .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(BehaviorTreeComponent)));

        // Register all conditions
        var conditionsTvm = new TreeViewItemViewModel()
        {
            TreeViewName = "Conditions",
            IconKind = "Bootstrap.QuestionOctagonFill",
        };
        AddItem("conditions", conditionsTvm, parent: componentTvm);

        var questConditions = new TreeViewItemViewModel()
        {
            TreeViewName = "Quest",
            IconKind = "Material.Script",
        };
        AddItem("quest_conditions", questConditions, parent: conditionsTvm);

        var enemyConditions = new TreeViewItemViewModel()
        {
            TreeViewName = "Enemy",
            IconKind = "Material.SwordCross",
        };
        AddItem("enemy_conditions", enemyConditions, parent: conditionsTvm);

        foreach (Type conditionType in componentTypes.Where(e => e.IsSubclassOf(typeof(ConditionComponent))))
        {
            var elem = new ComponentTreeViewItemViewModel(conditionType)
            {
                TreeViewName = conditionType.Name,
                IconKind = "Material.Memory",
            };

            if (conditionType.IsSubclassOf(typeof(QuestConditionComponent)))
            {
                AddItem(conditionType.Name, elem, parent: questConditions);
            }
            else if (conditionType.Name.StartsWith("Em"))
            {
                AddItem(conditionType.Name, elem, parent: enemyConditions);
            }
            else
            {
                AddItem(conditionType.Name, elem, parent: conditionsTvm);
            }
        }

        // Register all actions
        var actionsTvm = new TreeViewItemViewModel()
        {
            TreeViewName = "Actions",
            IconKind = "Bootstrap.ExclamationTriangleFill",
        };
        AddItem("actions", actionsTvm, parent: componentTvm);

        var questActions = new TreeViewItemViewModel()
        {
            TreeViewName = "Quest",
            IconKind = "Material.Script",
        };
        AddItem("quest_actions", questActions, parent: actionsTvm);

        var enemyActions = new TreeViewItemViewModel()
        {
            TreeViewName = "Enemy",
            IconKind = "Material.SwordCross",
        };
        AddItem("enemy_actions", enemyActions, parent: actionsTvm);

        foreach (Type actionType in componentTypes.Where(e => e.IsSubclassOf(typeof(ActionComponent))))
        {
            var elem = new ComponentTreeViewItemViewModel(actionType)
            {
                TreeViewName = actionType.Name,
                IconKind = "Material.Memory",
            };

            if (actionType.IsSubclassOf(typeof(QuestActionComponent)))
            {
                AddItem(actionType.Name, elem, parent: questActions);
            }
            else if (actionType.Name.StartsWith("Em"))
            {
                AddItem(actionType.Name, elem, parent: enemyActions);
            }
            else
            {
                AddItem(actionType.Name, elem, parent: actionsTvm);
            }
        }
    }


    public void AddItem(string id, TreeViewItemViewModel item, TreeViewItemViewModel? parent = null)
    {
        if (_idToItem.TryAdd(id, item))
        {
            if (parent is null)
                DisplayedItems.Add(item);
            else
                parent.DisplayedItems.Add(item);
        }
    }

    public void Clear()
    {
        _idToItem.Clear();
        DisplayedItems.Clear();
    }

    [RelayCommand]
    public void OnItemSelected(object obj)
    {
        switch (obj)
        {
            case FSMTreeViewItemViewModel fsm:
                //_editorViewModel.FSM = fsm.FSM;
                //_editorViewModel.Load();
                break;
        }
    }

    /*
    public void StartDrag(IDragInfo dragInfo)
    {
        GongSolutions.Wpf.DragDrop.DragDrop.DefaultDragHandler.StartDrag(dragInfo);
    }

    public bool CanStartDrag(IDragInfo dragInfo)
    {
        if (dragInfo.SourceItem is not TreeViewItemViewModel item)
            return false;

        if (item.DisplayedItems.Count > 0)
            return false;

        return GongSolutions.Wpf.DragDrop.DragDrop.DefaultDragHandler.CanStartDrag(dragInfo);
    }

    public void Dropped(IDropInfo dropInfo)
    {
        GongSolutions.Wpf.DragDrop.DragDrop.DefaultDragHandler.Dropped(dropInfo);
    }

    public void DragDropOperationFinished(DragDropEffects operationResult, IDragInfo dragInfo)
    {
        GongSolutions.Wpf.DragDrop.DragDrop.DefaultDragHandler.DragDropOperationFinished(operationResult, dragInfo);
    }
    */

    public void DragCancelled()
    {
    }
}
