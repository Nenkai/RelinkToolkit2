using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using Dock.Model.Controls;
using Dock.Model.Core;

using GBFRDataTools.Entities.Quest;
using GBFRDataTools.FSM;
using GBFRDataTools.FSM.Components.Actions.Quest;
using GBFRDataTools.FSM.Entities;

using RelinkToolkit2.Messages;
using RelinkToolkit2.Messages.IO;
using RelinkToolkit2.ViewModels.Documents;
using RelinkToolkit2.ViewModels.TreeView;

using System;
using System.IO;

namespace RelinkToolkit2.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public DocumentsViewModel DocumentsViewModel { get; }
    public TopMenuViewModel TopMenuViewModel { get; }
    public SolutionExplorerViewModel SolutionExplorerViewModel { get; }

    private readonly DockFactory? _factory;

    [ObservableProperty]
    private IRootDock? _layout;

    public MainViewModel(TopMenuViewModel topMenuViewModel, DocumentsViewModel documentsViewModel, SolutionExplorerViewModel solExplorerViewModel, DockFactory dockFactory)
    {
        TopMenuViewModel = topMenuViewModel;
        DocumentsViewModel = documentsViewModel;
        SolutionExplorerViewModel = solExplorerViewModel;

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
            int questId = questInfo.GetQuestId();

            var questTreeItem = new TreeViewItemViewModel()
            {
                Id = $"quest_{questId:X6}",
                TreeViewName = $"Quest ({questId:X6})",
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

                    for (int i = 0; i < questInfo.FsmDataList.Count; i++)
                    {
                        FsmDataInfo fsmFile = questInfo.FsmDataList[i];
                        string fsmFileName = $"quest_{questId:x}_{fsmFile.Suffix:x}_fsm_ingame.msg";
                        string fsmFilePath = Path.Combine(baseGameDir.FullName, "system", "fsm", "quest", fsmFileName);
                        byte[] questFsmFileBuf = File.ReadAllBytes(fsmFilePath);

                        string id = $"{questId:X6}_fsm_{fsmFile.Suffix}";
                        string name = $"[{i}] {fsmFile.Name}";

                        var parser = new FSMParser();
                        parser.Parse(questFsmFileBuf, asMessagePack: true);

                        FsmEditorViewModel editorViewModel = AddNewFSMDocument(id, name, parser);

                        fsmRoot.DisplayedItems.Add(new FSMTreeViewItemViewModel()
                        {
                            Id = $"{questId:X6}_fsm_{fsmFile.Suffix}",
                            TreeViewName = $"[{i}] {fsmFile.Name}",
                            FsmEditor = editorViewModel,
                        });
                    }

                    SolutionExplorerViewModel.AddItem($"quest_{questId:X6}", questTreeItem);
                }
            }
        }
        else
        {
            var parser = new FSMParser();
            parser.Parse(buffer, fileResult.Uri.LocalPath.EndsWith(".msg"));

            string fsmName = Path.GetFileNameWithoutExtension(fileResult.Uri.LocalPath).Replace("_fsm_ingame", string.Empty);
            string identifier = fsmName;

            FsmEditorViewModel editorViewModel = AddNewFSMDocument(identifier, fsmName, parser);
            var fsmTreeItem = new FSMTreeViewItemViewModel()
            {
                Id = identifier,
                TreeViewName = fsmName,
                FsmEditor = editorViewModel,
                IsExpanded = true,
            };

            foreach (var layerGroup in editorViewModel.LayerGroups.Values)
            {
                if (layerGroup.LayerIndex == 0)
                    continue;

                fsmTreeItem.DisplayedItems.Add(new FSMLayerTreeViewItemViewModel()
                {
                    Id = $"{fsmTreeItem.Id}_layer{layerGroup.LayerIndex}",
                    TreeViewName = $"Layer {layerGroup.LayerIndex}",
                    LayerGroup = layerGroup,
                });
            }

            SolutionExplorerViewModel.AddItem(fsmName, fsmTreeItem);
        }
    }

    /// <summary>
    /// Adds a new fsm document (won't add it if it already exists).
    /// </summary>
    /// <param name="identifier"></param>
    /// <param name="name"></param>
    /// <param name="fsmParser"></param>
    private FsmEditorViewModel AddNewFSMDocument(string identifier, string name, FSMParser fsmParser)
    {
        FsmEditorViewModel fsmEditorViewModel = new()
        {
            Id = identifier,
            Title = name,
            Documents = DocumentsViewModel,
        };
        fsmEditorViewModel.InitGraph(name, fsmParser);

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
