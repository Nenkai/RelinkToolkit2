using System;
using System.IO;

using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.ComponentModel;

using Dock.Model.Controls;
using Dock.Model.Core;

using RelinkToolkit2.ViewModels.Documents;
using RelinkToolkit2.Messages.IO;
using RelinkToolkit2.Messages;
using RelinkToolkit2.Messages.Fsm;

using GBFRDataTools.FSM.Entities;
using GBFRDataTools.FSM.Components.Actions.Quest;
using GBFRDataTools.Entities.Quest;
using GBFRDataTools.FSM;

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

        WeakReferenceMessenger.Default.Register<OpenFsmDocumentRequest>(this, (recipient, message) =>
        {
            if (!DocumentsViewModel.IsDocumentOpen(message.Id))
                AddNewFSMDocument(message.Id, message.Name, message.FSM);
            else
            {
                DocumentsViewModel.SetActiveDocument(message.Id);
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

                        var parser = new FSMParser();
                        parser.Parse(questFsmFileBuf, asMessagePack: true);

                        fsmRoot.DisplayedItems.Add(new FSMTreeViewItemViewModel()
                        {
                            Id = $"{questId:X6}_fsm_{fsmFile.Suffix}",
                            FSM = parser,
                            TreeViewName = $"[{i}] {fsmFile.Name}",
                            IconKind = "Material.ChartTimelineVariant",
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

            AddNewFSMDocument(identifier, fsmName, parser);
            var fsm = new FSMTreeViewItemViewModel()
            {
                Id = identifier,
                TreeViewName = fsmName,
                IconKind = "Material.ChartTimelineVariant",
                FSM = parser,
            };

            SolutionExplorerViewModel.AddItem(fsmName, fsm);
        }
    }

    /// <summary>
    /// Adds a new fsm document (won't add it if it already exists).
    /// </summary>
    /// <param name="identifier"></param>
    /// <param name="name"></param>
    /// <param name="fsm"></param>
    private void AddNewFSMDocument(string identifier, string name, FSMParser fsm)
    {
        var fsmEditorViewModel = new FsmEditorViewModel
        {
            Id = identifier,
            Title = name,
            FSM = fsm,
            Documents = DocumentsViewModel,
        };
        fsmEditorViewModel.InitGraph(name);

        DocumentsViewModel.AddDocument(fsmEditorViewModel);
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
