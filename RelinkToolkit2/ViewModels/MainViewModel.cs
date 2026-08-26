using Avalonia.Controls;
using Avalonia.Platform.Storage;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

using Dock.Model.Controls;

using GBFRDataTools.Entities;
using GBFRDataTools.Entities.Quest;
using GBFRDataTools.FSM;
using GBFRDataTools.FSM.Components.Actions;

using MessagePack.Formatters;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using MsBox.Avalonia;
using MsBox.Avalonia.Models;

using NLog.Filters;

using RelinkToolkit2.Messages.Documents;
using RelinkToolkit2.Messages.IO;
using RelinkToolkit2.Services;
using RelinkToolkit2.ViewModels.Documents;
using RelinkToolkit2.ViewModels.Documents.GraphEditor;
using RelinkToolkit2.ViewModels.Documents.GraphEditor.Nodes;
using RelinkToolkit2.ViewModels.TreeView;
using RelinkToolkit2.Views.Tools;

using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RelinkToolkit2.ViewModels;

public partial class MainViewModel : ObservableObject,
    IRecipient<FileOpenRequestMessage>,
    IRecipient<DockLayoutResetRequest>,
    IRecipient<OpenDocumentRequest>
{
    private readonly ILoggerFactory? _loggerFactory;
    private readonly ILogger? _logger;
    private readonly DialogService _dialogService;

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
        DialogService dialogService,
        ILogger<MainViewModel> logger)
    {
        TopMenuViewModel = topMenuViewModel;
        DocumentsViewModel = documentsViewModel;
        SolutionExplorerViewModel = solExplorerViewModel;
        StatusBarViewModel = statusBarViewModel;

        _factory = dockFactory;
        _loggerFactory = loggerFactory;
        _logger = logger;
        _dialogService = dialogService;

        Layout = dockFactory.CreateLayout();
        if (Layout is not null)
        {
            _logger?.LogInformation("Initializing dock layout");
            dockFactory.InitLayout(Layout);
        }

        WeakReferenceMessenger.Default.RegisterAll(this);
    }

    /// <summary>
    /// Fired when a file is opened.
    /// </summary>
    /// <param name="message"></param>
    public async void Receive(FileOpenRequestMessage message)
    {
        await LoadFile(message.Value.Uri);
    }

    /// <summary>
    /// Fired when dock layout is resetted.
    /// </summary>
    /// <param name="message"></param>
    public void Receive(DockLayoutResetRequest message)
    {
        ResetLayout();
        message.Reply(true);
    }

    /// <summary>
    /// Fired when a new document has been opened.
    /// </summary>
    /// <param name="message"></param>
    public void Receive(OpenDocumentRequest message)
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
    }

    public async Task LoadFile(Uri fileResult)
    {
        _logger?.LogInformation("Load file request: {}", fileResult);

        string fileName = fileResult.OriginalString;
        if (fileName.Contains("fsm_ingame"))
        {
            await ProcessFSM(fileResult);
        }
        else if (fileName.Contains("behavior_tree_ingame"))
        {
            await ProcessBT(fileResult);
        }
        else if (fileName.Contains("baseinfo"))
        {
            await ProcessBaseInfo(fileResult);
        }
        else
        {
            string path = fileResult.LocalPath;

            try
            {

                var entity = GenericEntitySerializer.Parse(File.ReadAllBytes(path), path.EndsWith(".msg"));
                GenericEntityEditorViewModel editorViewModel = new()
                {
                    Id = Path.GetFileNameWithoutExtension(path),
                    Title = Path.GetFileNameWithoutExtension(path),
                    Documents = DocumentsViewModel,
                };
                editorViewModel.SetObjects(entity);

                DocumentsViewModel.AddDocument(editorViewModel);
                editorViewModel.LastFile = fileResult;
            }
            catch (Exception ex)
            {
                var box = MessageBoxManager.GetMessageBoxStandard("Error", $"Failed to load file.\n{ex.Message}", icon: MsBox.Avalonia.Enums.Icon.Error);
                await _dialogService.ShowMessageBoxDialogAsync(box);
                return;
            }
        }
    }

    private async Task ProcessFSM(Uri uri)
    {
        FSMParser parser;
        try
        {

            parser = new FSMParser(_loggerFactory);
            await parser.Parse(File.ReadAllBytes(uri.LocalPath), uri.LocalPath.EndsWith(".msg"), OnLocateFSMFile);
            if (parser.HasErrors)
            {
                await PromptFSMErrored(uri);

                _logger?.LogWarning("FSM Parsed with errors. ({file})", uri.LocalPath);
            }
            else
                _logger?.LogInformation("FSM Parsed ({file}).", uri.LocalPath);
        }
        catch (Exception ex)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", $"Failed to load FSM file.\n{ex.Message}", icon: MsBox.Avalonia.Enums.Icon.Error);
            await _dialogService.ShowMessageBoxDialogAsync(box);
            return;
        }

        string fsmName = Path.GetFileNameWithoutExtension(uri.LocalPath).Replace("_fsm_ingame", string.Empty);

        uint questId = 0;
        uint progressIndex = 0;
        if (fsmName.StartsWith("quest_"))
        {
            string[] spl = fsmName.Split('_');
            if (spl.Length < 3 || !uint.TryParse(spl[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out questId))
            {
                var box = MessageBoxManager.GetMessageBoxStandard("Error", $"Failed to parse quest id from quest fsm file name '{fsmName}'\nMake sure the file name is correct.", icon: MsBox.Avalonia.Enums.Icon.Error);
                await _dialogService.ShowMessageBoxDialogAsync(box);
                return;
            }

            if (!uint.TryParse(spl[2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out progressIndex))
            {
                var box = MessageBoxManager.GetMessageBoxStandard("Error", $"Failed to parse progress index from quest fsm file name '{fsmName}'\nMake sure the file name is correct.", icon: MsBox.Avalonia.Enums.Icon.Error);
                await _dialogService.ShowMessageBoxDialogAsync(box);
                return;
            }
        }

        string editorIdentifier = $"fsm_{fsmName}";

        FsmEditorViewModel? editorViewModel = await AddNewFSMDocument(editorIdentifier, fsmName, parser);
        if (editorViewModel is null)
            return;

        editorViewModel.LastFile = uri;

        if (fsmName.StartsWith("quest_"))
        {
            foreach (NodeViewModel node in editorViewModel.Nodes)
            {
                foreach (NodeComponentViewModel componentVM in node.Components)
                {
                    if (componentVM.Component is QuestActionComponent questActionComponent)
                    {
                        editorViewModel.SetQuestContext(questActionComponent.Category,
                            questActionComponent.Subcategory,
                            questActionComponent.Index,
                            questActionComponent.ProgressIndex,
                            questActionComponent.ProgressHash);
                        goto exitLoop;
                    }
                }
            }
        }

    exitLoop:
        if (!editorViewModel.HasQuestContext())
        {
            editorViewModel.SetQuestContext(
                category: (questId >> 20) & 0xF,
                subCategory: (questId >> 12) & 0xFF,
                index: (questId & 0xFFF),
                progressIndex: progressIndex,
                progressHash: (ulong)Random.Shared.NextInt64());
        }

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

        if (parser.BaseFSMParser is not null)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Warning", $"This FSM uses a template/base FSM and saving is not supported, do not save it.", icon: MsBox.Avalonia.Enums.Icon.Warning);
            await _dialogService.ShowMessageBoxDialogAsync(box);
        }
    }

    public async Task<string?> OnLocateFSMFile(string className, string fsmName)
    {
        var filesService = App.Current?.Services?.GetService<IFilesService>();
        if (filesService is null)
            return null;

        string file = $"{className}_{fsmName}_fsm_ingame.msg";
        string fileNoExtension = Path.GetFileNameWithoutExtension(file);
        var localFile = await filesService.OpenFileAsync($"Locate template FSM {file}", filters:
        [
            new FilePickerFileType("FSM File")
            {
                Patterns = [$"{fileNoExtension}.msg", $"{fileNoExtension}.json"]
            },
            FilePickerFileTypes.All,
        ]);
        
        return localFile?.Path?.LocalPath;
    }

    private async Task PromptFSMErrored(Uri uri)
    {
        var box = MessageBoxManager.GetMessageBoxCustom(new MsBox.Avalonia.Dto.MessageBoxCustomParams()
        {
            ContentTitle = "Error",
            ContentMessage = $"FSM '{Path.GetFileNameWithoutExtension(uri.LocalPath)}' has loaded with errors, check the log window for more information.\n" +
                                "Do not use it for saving.",
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Icon = MsBox.Avalonia.Enums.Icon.Error,
            ButtonDefinitions =
            [
                new ButtonDefinition()
                {
                    Name = "Open Logs",
                    IsCancel = false,
                },
                new ButtonDefinition()
                {
                    Name = "OK",
                    IsDefault = true,
                    IsCancel = true,
                }
            ],
        });

        var res = await _dialogService.ShowMessageBoxDialogAsync(box);
        if (res == "Open Logs")
        {
            var logWindow = new LogWindow();
            logWindow.Show();
        }
    }

    private async Task<bool> ProcessBaseInfo(Uri uri)
    {
        byte[] buffer = File.ReadAllBytes(uri.LocalPath);

        var questInfo = BaseInfo.Read(buffer);
        uint questIdNumber = questInfo.GetQuestId();

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

        SolutionExplorerViewModel.AddItem(questTreeItem);

        string path = uri.LocalPath;
        string? dirName = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dirName))
        {
            DirectoryInfo questDir = new(dirName);

            if (questDir.Parent?.Parent is not null)
            {
                DirectoryInfo baseGameDir = questDir.Parent.Parent;
                bool isEx = Path.GetFileNameWithoutExtension(questDir.Parent.FullName) == "ex";
                if (isEx)
                    baseGameDir = baseGameDir.Parent;

                SolutionExplorerViewModel.AddItem(fsmRoot, questTreeItem.Guid);

                for (int i = 0; i < questInfo.FsmDataList.Count; i++)
                {
                    FsmDataInfo fsmDataRefInfo = questInfo.FsmDataList[i];
                    string fsmFileName;
                    if (isEx)
                        fsmFileName = $"quest_{questIdNumber:x}_{fsmDataRefInfo.Suffix:x}_ex_fsm_ingame.msg";
                    else
                        fsmFileName = $"quest_{questIdNumber:x}_{fsmDataRefInfo.Suffix:x}_fsm_ingame.msg";

                    string fsmFilePath = Path.Combine(baseGameDir.FullName, "system", "fsm", "quest", fsmFileName);
                    byte[] questFsmFileBuf = File.ReadAllBytes(fsmFilePath);

                    string fsmIdentifier = $"fsm_{questIdNumber:X6}_{fsmDataRefInfo.Suffix}";
                    string fsmName = $"[{i}] {fsmDataRefInfo.Name}";

                    try
                    {
                        var parser = new FSMParser(_loggerFactory);
                        await parser.Parse(questFsmFileBuf, asMessagePack: true, OnLocateFSMFile);

                        if (parser.HasErrors)
                        {
                            await PromptFSMErrored(uri);
                        }

                        _logger?.LogInformation("Parsed quest FSM ({fsmFileName}).", fsmFileName);

                        FsmEditorViewModel? editorViewModel = await AddNewFSMDocument(fsmIdentifier, fsmName, parser);
                        if (editorViewModel is null)
                            continue;

                        editorViewModel.SetQuestContext(questInfo.Category, questInfo.SubCategory, questInfo.SerialNumber, (uint)i, fsmDataRefInfo.Hash);
                        if (editorViewModel is not null)
                        {
                            SolutionExplorerViewModel.AddItem(new FSMTreeViewItemViewModel()
                            {
                                TreeViewName = $"[{i}] {fsmDataRefInfo.Name}",
                                FsmEditor = editorViewModel,
                            }, parentId: fsmRoot.Guid);
                        }
                    }
                    catch (Exception ex)
                    {
                        var box = MessageBoxManager.GetMessageBoxStandard("Error", $"Failed to load FSM file.\n{ex.Message}", icon: MsBox.Avalonia.Enums.Icon.Error);
                        await _dialogService.ShowMessageBoxDialogAsync(box);
                        return false;
                    }
                }
            }
        }

        return true;
    }

    private async Task ProcessBT(Uri uri)
    {
        BTParser parser;
        try
        {

            parser = new BTParser(_loggerFactory);
            parser.Parse(File.ReadAllBytes(uri.LocalPath), uri.LocalPath.EndsWith(".msg"));
            if (parser.HasErrors)
                await PromptFSMErrored(uri);

            _logger?.LogInformation("FSM Parsed ({file}).", uri.LocalPath);
        }
        catch (Exception ex)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", $"Failed to load FSM file.\n{ex.Message}", icon: MsBox.Avalonia.Enums.Icon.Error);
            await _dialogService.ShowMessageBoxDialogAsync(box);
            return;
        }

        string fsmName = Path.GetFileNameWithoutExtension(uri.LocalPath).Replace("_fsm_ingame", string.Empty);
        string fsmId = $"fsm_{fsmName}";

        BTEditorViewModel? editorViewModel = AddNewBTDocument(fsmId, fsmName, parser);
        if (editorViewModel is null)
            return;
        editorViewModel.LastFile = uri;

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

    public async Task NewFSMDocument()
    {
        string fsmName = "new_fsm";
        FsmEditorViewModel? editorViewModel = await AddNewFSMDocument(fsmName, fsmName);
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
    private async Task<FsmEditorViewModel?> AddNewFSMDocument(string identifier, string name, FSMParser? fsmParser = null)
    {
        FsmEditorViewModel fsmEditorViewModel = new()
        {
            Id = identifier,
            Title = name,
            Documents = DocumentsViewModel,
        };

        if (!await fsmEditorViewModel.InitGraph(name, fsmParser))
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
