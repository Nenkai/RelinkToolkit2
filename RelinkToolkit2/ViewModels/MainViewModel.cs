using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using Dock.Model.Controls;
using Dock.Model.Core;

using GBFRDataTools.Entities.Quest;
using GBFRDataTools.FSM;
using GBFRDataTools.FSM.Components.Actions.Quest;
using GBFRDataTools.FSM.Entities;

using MsBox.Avalonia;

using RelinkToolkit2.Messages.Dialogs;
using RelinkToolkit2.Messages.Documents;
using RelinkToolkit2.Messages.IO;
using RelinkToolkit2.ViewModels.Documents;
using RelinkToolkit2.ViewModels.Fsm;
using RelinkToolkit2.ViewModels.TreeView;

using System;
using System.IO;

namespace RelinkToolkit2.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public TopMenuViewModel TopMenuViewModel { get; }
    public DocumentsViewModel DocumentsViewModel { get; }
    public SolutionExplorerViewModel SolutionExplorerViewModel { get; }
    public StatusBarViewModel StatusBarViewModel { get; }
    private readonly DockFactory? _factory;

    [ObservableProperty]
    private IRootDock? _layout;

    public MainViewModel(TopMenuViewModel topMenuViewModel,
        DocumentsViewModel documentsViewModel, 
        SolutionExplorerViewModel solExplorerViewModel, 
        StatusBarViewModel statusBarViewModel,
        DockFactory dockFactory)
    {
        TopMenuViewModel = topMenuViewModel;
        DocumentsViewModel = documentsViewModel;
        SolutionExplorerViewModel = solExplorerViewModel;
        StatusBarViewModel = statusBarViewModel;

        _factory = dockFactory;

        Layout = dockFactory.CreateLayout();
        if (Layout is not null)
            dockFactory.InitLayout(Layout);

        WeakReferenceMessenger.Default.Register<FileOpenRequestMessage>(this, (recipient, message) =>
        {
            ProcessFileLoadedChanged(message.Value);
        });

        WeakReferenceMessenger.Default.Register<DockLayoutResetRequest>(this, (recipient, message) =>
        {
            ResetLayout();
            message.Reply(true);
        });

        WeakReferenceMessenger.Default.Register<OpenDocumentRequest>(this, (recipient, message) =>
        {
            if (!DocumentsViewModel.IsDocumentOpen(message.Document.Id))
                AddNewFSMDocument(message.Document.Id, message.Document.Title, null);
            else
            {
                var currentDocument = DocumentsViewModel.ActiveDockable;
                if (currentDocument is not null && currentDocument.Id != message.Document.Id)
                {
                    DocumentsViewModel.SetActiveDocument(message.Document.Id);
                }
            }

            message.Reply(true);
        });
    }

    public void ProcessFileLoadedChanged(FileOpenResult fileResult)
    {
        byte[] buffer = new byte[fileResult.Stream.Length];
        fileResult.Stream.ReadExactly(buffer);

        string fileName = Path.GetFileNameWithoutExtension(fileResult.Uri.OriginalString);
        if (fileName.Contains("baseinfo", StringComparison.Ordinal))
        {
            var questInfo = BaseInfo.Read(buffer);
            int questIdNumber = questInfo.GetQuestId();

            var questTreeItem = new TreeViewItemViewModel()
            {
                TreeViewName = $"Quest ({questIdNumber:X6})",
                IconKind = "Material.Script",
                IsExpanded = true,
            };

            var fsmRoot = new TreeViewItemViewModel()
            {
                TreeViewName = "FSM",
                IconKind = "Material.Graph",
                IsExpanded = true,
            };

            questTreeItem.DisplayedItems.Add(fsmRoot);

            string path = fileResult.Uri.LocalPath;
            string? dirName = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dirName))
            {
                DirectoryInfo questDir = new(dirName);

                if (questDir.Parent?.Parent is not null)
                {
                    DirectoryInfo baseGameDir = questDir.Parent.Parent;

                    SolutionExplorerViewModel.AddItem(questTreeItem);

                    for (int i = 0; i < questInfo.FsmDataList.Count; i++)
                    {
                        FsmDataInfo fsmFile = questInfo.FsmDataList[i];
                        string fsmFileName = $"quest_{questIdNumber:x}_{fsmFile.Suffix:x}_fsm_ingame.msg";
                        string fsmFilePath = Path.Combine(baseGameDir.FullName, "system", "fsm", "quest", fsmFileName);
                        byte[] questFsmFileBuf = File.ReadAllBytes(fsmFilePath);

                        string fsmId = $"fsm_{questIdNumber:X6}_{fsmFile.Suffix}";
                        string fsmName = $"[{i}] {fsmFile.Name}";

                        var parser = new FSMParser();
                        parser.Parse(questFsmFileBuf, asMessagePack: true);

                        FsmEditorViewModel? editorViewModel = AddNewFSMDocument(fsmId, fsmName, parser);
                        if (editorViewModel is not null)
                        {
                            SolutionExplorerViewModel.AddItem(new FSMTreeViewItemViewModel()
                            {
                                TreeViewName = $"[{i}] {fsmFile.Name}",
                                FsmEditor = editorViewModel,
                            }, parentId: questTreeItem.Guid);
                        }
                    }
                }
            }
        }
        else
        {
            FSMParser parser;
            try
            {

                parser = new FSMParser();
                parser.Parse(File.ReadAllBytes(fileResult.Uri.LocalPath), fileResult.Uri.LocalPath.EndsWith(".msg"));
            }
            catch (Exception ex)
            {
                var box = MessageBoxManager.GetMessageBoxStandard("Error", $"Failed to load FSM file.\n{ex.Message}", icon: MsBox.Avalonia.Enums.Icon.Error);
                WeakReferenceMessenger.Default.Send(new ShowDialogRequest(box));
                return;
            }

            string fsmName = Path.GetFileNameWithoutExtension(fileResult.Uri.LocalPath).Replace("_fsm_ingame", string.Empty);
            string fsmId = $"fsm_{fsmName}";

            FsmEditorViewModel? editorViewModel = AddNewFSMDocument(fsmId, fsmName, parser);
            if (editorViewModel is null)
                return;

            var fsmTreeItem = new FSMTreeViewItemViewModel()
            {
                TreeViewName = fsmName,
                FsmEditor = editorViewModel,
                IsExpanded = true,
            };
            editorViewModel.SolutionTreeViewItem = fsmTreeItem;
            SolutionExplorerViewModel.AddItem(fsmTreeItem);

            foreach (GroupNodeViewModel layerGroup in editorViewModel.LayerGroups.Values)
            {
                if (layerGroup.LayerIndex == 0)
                    continue;

                var layerItem = new FSMLayerTreeViewItemViewModel()
                {
                    TreeViewName = layerGroup.Title ?? $"Layer {layerGroup.LayerIndex}",
                    LayerGroup = layerGroup,
                    Caption = $"Layer {layerGroup.LayerIndex}",
                    Guid = layerGroup.Id, // Set as the one created by group
                };

                SolutionExplorerViewModel.AddItem(layerItem, fsmTreeItem.Guid);
            }
        }
    }

    /// <summary>
    /// Adds a new fsm document and initializes it (won't add it if it already exists).
    /// </summary>
    /// <param name="identifier"></param>
    /// <param name="name"></param>
    /// <param name="fsmParser"></param>
    /// <returns>Editor view model. <see cref="null"/> if it wasn't loaded.</returns>
    private FsmEditorViewModel? AddNewFSMDocument(string identifier, string name, FSMParser fsmParser)
    {
        FsmEditorViewModel fsmEditorViewModel = new()
        {
            Id = identifier,
            Title = name,
            Documents = DocumentsViewModel,
        };

        if (!fsmEditorViewModel.InitGraph(name, fsmParser))
            return null;

        DocumentsViewModel.AddDocument(fsmEditorViewModel);
        return fsmEditorViewModel;
    }

    /// <summary>
    /// Resets the dock layout.
    /// </summary>
    public void ResetLayout()
    {
        if (Layout is not null)
        {
            if (Layout.Close.CanExecute(null))
            {
                Layout.Close.Execute(null);
            }
        }

        var layout = _factory?.CreateLayout();
        if (layout is not null)
        {
            Layout = layout;
            _factory?.InitLayout(layout);
        }
    }

}
