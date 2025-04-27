using Aldwych.Logging;
using Aldwych.Logging.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using Dock.Model.Controls;
using Dock.Model.Core;

using DynamicData.Binding;

using GBFRDataTools.Entities;
using GBFRDataTools.Entities.Player;
using GBFRDataTools.Entities.Quest;
using GBFRDataTools.FSM;
using GBFRDataTools.FSM.Components.Actions.Quest;
using GBFRDataTools.FSM.Entities;

using Microsoft.Extensions.Logging;

using MsBox.Avalonia;

using RelinkToolkit2.Messages.Dialogs;
using RelinkToolkit2.Messages.Documents;
using RelinkToolkit2.Messages.IO;
using RelinkToolkit2.ViewModels.Documents;
using RelinkToolkit2.ViewModels.Documents.GraphEditor;
using RelinkToolkit2.ViewModels.Documents.GraphEditor.Nodes;
using RelinkToolkit2.ViewModels.TreeView;

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml.Linq;

namespace RelinkToolkit2.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private ILoggerFactory? _loggerFactory;
    private ILogger? _logger;

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
        DockFactory dockFactory,

        ILoggerFactory loggerFactory,
        ILogger<MainViewModel> logger)
    {
        TopMenuViewModel = topMenuViewModel;
        DocumentsViewModel = documentsViewModel;
        SolutionExplorerViewModel = solExplorerViewModel;
        StatusBarViewModel = statusBarViewModel;

        _factory = dockFactory;

        _loggerFactory = loggerFactory;
        _logger = logger;

        Layout = dockFactory.CreateLayout();
        if (Layout is not null)
            dockFactory.InitLayout(Layout);

        WeakReferenceMessenger.Default.Register<FileOpenRequestMessage>(this, (recipient, message) =>
        {
            LoadFile(message.Value.Uri);
        });

        WeakReferenceMessenger.Default.Register<DockLayoutResetRequest>(this, (recipient, message) =>
        {
            ResetLayout();
            message.Reply(true);
        });

        WeakReferenceMessenger.Default.Register<OpenDocumentRequest>(this, (recipient, message) =>
        {
            if (!DocumentsViewModel.IsDocumentOpen(message.Document.Id))
            {
                DocumentsViewModel.AddDocument(message.Document);
            }
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

    public void LoadFile(Uri fileResult)
    {
        string fileName = fileResult.OriginalString;
        if (fileName.Contains("fsm_ingame"))
        {
            ProcessFSM(fileResult);
        }
        else if (fileName.Contains("behavior_tree_ingame"))
        {
            ProcessBT(fileResult);
        }
        else
        {
            string path = fileResult.LocalPath;

            try
            {
                /*
                foreach (var file in Directory.GetFiles(@"D://Games/SteamLibrary/steamapps/common/Granblue Fantasy Relink/extracted/system/enemy/data", "*", SearchOption.AllDirectories))
                {
                    if (!file.Contains("we"))
                        continue;

                    if (file.EndsWith(".json"))
                        continue;

                    if (!file.EndsWith("parameter.msg"))
                        continue;
                    */

                    var entity = GenericEntitySerializer.Parse(File.ReadAllBytes(path), path.EndsWith(".msg"));
                    GenericEntityEditorViewModel editorViewModel = new()
                    {
                        Id = Path.GetFileNameWithoutExtension(path),
                        Title = Path.GetFileNameWithoutExtension(path),
                        Documents = DocumentsViewModel,
                    };
                    editorViewModel.SetObjects(entity);
                //}

                DocumentsViewModel.AddDocument(editorViewModel);
            }
            catch (Exception ex)
            {
                var box = MessageBoxManager.GetMessageBoxStandard("Error", $"Failed to load file.\n{ex.Message}", icon: MsBox.Avalonia.Enums.Icon.Error);
                WeakReferenceMessenger.Default.Send(new ShowDialogRequest(box));
                return;
            }
        }
    }

    private void ProcessFSM(Uri uri)
    {
        byte[] buffer = File.ReadAllBytes(uri.LocalPath);

        string fileName = Path.GetFileNameWithoutExtension(uri.OriginalString);
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

            string path = uri.LocalPath;
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

                        try
                        {
                            var parser = new FSMParser();
                            parser.Parse(questFsmFileBuf, asMessagePack: true);
                            if (parser.HasErrors)
                            {
                                var box = MessageBoxManager.GetMessageBoxStandard("Error", $"FSM '{fsmFileName}' has loaded with errors, check the log window for more information.\n" +
                                    $"Do not use it for saving.", icon: MsBox.Avalonia.Enums.Icon.Error);
                                WeakReferenceMessenger.Default.Send(new ShowDialogRequest(box));
                            }

                            _logger?.LogInformation("Parsed quest FSM ({fsmFileName}).", fsmFileName);

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
                        catch (Exception ex)
                        {
                            var box = MessageBoxManager.GetMessageBoxStandard("Error", $"Failed to load FSM file.\n{ex.Message}", icon: MsBox.Avalonia.Enums.Icon.Error);
                            WeakReferenceMessenger.Default.Send(new ShowDialogRequest(box));
                            return;
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

                parser = new FSMParser(_loggerFactory);
                parser.Parse(File.ReadAllBytes(uri.LocalPath), uri.LocalPath.EndsWith(".msg"));
                if (parser.HasErrors)
                {
                    var box = MessageBoxManager.GetMessageBoxStandard("Error",
                        $"FSM '{Path.GetFileNameWithoutExtension(uri.LocalPath)}' has loaded with errors, check the log window for more information.\n" +
                        "Do not use it for saving.", icon: MsBox.Avalonia.Enums.Icon.Error);
                    WeakReferenceMessenger.Default.Send(new ShowDialogRequest(box));
                }

                _logger?.LogInformation("FSM Parsed ({file}).", uri.LocalPath);
            }
            catch (Exception ex)
            {
                var box = MessageBoxManager.GetMessageBoxStandard("Error", $"Failed to load FSM file.\n{ex.Message}", icon: MsBox.Avalonia.Enums.Icon.Error);
                WeakReferenceMessenger.Default.Send(new ShowDialogRequest(box));
                return;
            }

            string fsmName = Path.GetFileNameWithoutExtension(uri.LocalPath).Replace("_fsm_ingame", string.Empty);
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

            foreach (FsmGroupNodeViewModel layerGroup in editorViewModel.LayerGroups.Values)
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

    private void ProcessBT(Uri uri)
    {
        byte[] buffer = File.ReadAllBytes(uri.LocalPath);

        string fileName = Path.GetFileNameWithoutExtension(uri.OriginalString);

        BTParser parser;
        try
        {

            parser = new BTParser(_loggerFactory);
            parser.Parse(File.ReadAllBytes(uri.LocalPath), uri.LocalPath.EndsWith(".msg"));
            if (parser.HasErrors)
            {
                var box = MessageBoxManager.GetMessageBoxStandard("Error",
                    $"FSM '{Path.GetFileNameWithoutExtension(uri.LocalPath)}' has loaded with errors, check the log window for more information.\n" +
                    "Do not use it for saving.", icon: MsBox.Avalonia.Enums.Icon.Error);
                WeakReferenceMessenger.Default.Send(new ShowDialogRequest(box));
            }

            _logger?.LogInformation("FSM Parsed ({file}).", uri.LocalPath);
        }
        catch (Exception ex)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", $"Failed to load FSM file.\n{ex.Message}", icon: MsBox.Avalonia.Enums.Icon.Error);
            WeakReferenceMessenger.Default.Send(new ShowDialogRequest(box));
            return;
        }

        string fsmName = Path.GetFileNameWithoutExtension(uri.LocalPath).Replace("_fsm_ingame", string.Empty);
        string fsmId = $"fsm_{fsmName}";

        BTEditorViewModel? editorViewModel = AddNewBTDocument(fsmId, fsmName, parser);
        if (editorViewModel is null)
            return;

        var fsmTreeItem = new BTTreeViewItemViewModel()
        {
            TreeViewName = fsmName,
            FsmEditor = editorViewModel,
            IsExpanded = true,
        };
        editorViewModel.SolutionTreeViewItem = fsmTreeItem;
        SolutionExplorerViewModel.AddItem(fsmTreeItem);
    }

    public void NewBTDocument()
    {
        string btName = "new_bt";
        BTEditorViewModel? editorViewModel = AddNewBTDocument(btName, btName);
        if (editorViewModel is null)
            return;

        var btTreeItem = new BTTreeViewItemViewModel()
        {
            TreeViewName = btName,
            FsmEditor = editorViewModel,
            IsExpanded = true,
        };
        editorViewModel.SolutionTreeViewItem = btTreeItem;
        SolutionExplorerViewModel.AddItem(btTreeItem);
    }

    public void NewFSMDocument()
    {
        string fsmName = "new_fsm";
        FsmEditorViewModel? editorViewModel = AddNewFSMDocument(fsmName, fsmName);
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
    }

    /// <summary>
    /// Adds a new fsm document and initializes it (won't add it if it already exists).
    /// </summary>
    /// <param name="identifier"></param>
    /// <param name="name"></param>
    /// <param name="fsmParser"></param>
    /// <returns>Editor view model. <see cref="null"/> if it wasn't loaded.</returns>
    private FsmEditorViewModel? AddNewFSMDocument(string identifier, string name, FSMParser? fsmParser = null)
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
    /// Adds a new bt document and initializes it (won't add it if it already exists).
    /// </summary>
    /// <param name="identifier"></param>
    /// <param name="name"></param>
    /// <param name="fsmParser"></param>
    /// <returns>Editor view model. <see cref="null"/> if it wasn't loaded.</returns>
    private BTEditorViewModel? AddNewBTDocument(string identifier, string name, BTParser? btParser = null)
    {
        BTEditorViewModel btEditorViewModel = new()
        {
            Id = identifier,
            Title = name,
            Documents = DocumentsViewModel,
        };

        if (!btEditorViewModel.InitGraph(name, btParser))
            return null;

        DocumentsViewModel.AddDocument(btEditorViewModel);
        return btEditorViewModel;
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
