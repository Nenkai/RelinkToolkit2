using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Styling;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using RelinkToolkit2.Messages;
using RelinkToolkit2.Messages.Documents;
using RelinkToolkit2.Messages.Fsm;
using RelinkToolkit2.Messages.IO;
using RelinkToolkit2.Messages.StatusBar;
using RelinkToolkit2.Services;
using RelinkToolkit2.Services.Configuration.Models;
using RelinkToolkit2.Services.FSM;
using RelinkToolkit2.ViewModels.Documents;
using RelinkToolkit2.ViewModels.Documents.Interfaces;
using RelinkToolkit2.ViewModels.Menu;
using RelinkToolkit2.ViewModels.Windows;

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

namespace RelinkToolkit2.ViewModels;

public partial class TopMenuViewModel : ObservableObject,
    IRecipient<ActiveDocumentChangedMessage>
{
    private readonly ILogger? _logger;
    private readonly IDialogService _dialog;
    private readonly IFilesService _filesService;
    private readonly RecentFilesService _recentFiles;
    private readonly FSMFileService _fsmBtVerifierService;

    public ObservableCollection<IMenuItemViewModel> MenuItems { get; set; } = [];
    public ObservableCollection<MenuItemViewModel> _themeMenuItems = [];

    private MenuItemViewModel _saveMenuItem;
    private MenuItemViewModel _saveAsMenuItem;
    private MenuItemViewModel _recentItemsMenuItem;

    public TopMenuViewModel(ILogger<TopMenuViewModel>? logger,
        IDialogService dialogService,
        IFilesService filesService,
        FSMFileService fsmBtVerifierService,
        RecentFilesService recentFiles) 
    {
        _logger = logger;
        _dialog = dialogService;
        _filesService = filesService;
        _fsmBtVerifierService = fsmBtVerifierService;
        _recentFiles = recentFiles;

        BuildMenu();

        WeakReferenceMessenger.Default.RegisterAll(this);
    }

    private void OnRecentFilesUpdated(Collections.RecentItemsStack<Uri> items)
    {
        _recentItemsMenuItem.MenuItems.Clear();

        foreach (var item in items)
            AddRecentItem(item);
    }

    public void Receive(ActiveDocumentChangedMessage message)
    {
        EditorDocumentBase? doc = message.Value;
        bool isSaveable = doc is ISaveableDocument;
        _saveMenuItem!.Enabled = isSaveable;
        _saveAsMenuItem!.Enabled = isSaveable;

        if (isSaveable)
            _saveAsMenuItem!.Header = $"Save {doc!.Title} As...";
        else
            _saveAsMenuItem!.Header = $"Save As...";
    }

    public void BuildMenu()
    {
        MenuItemViewModel fileMenuItem = CreateFileMenu();
        MenuItems.Add(fileMenuItem);

        MenuItemViewModel viewMenuItem = CreateViewMenu();
        MenuItems.Add(viewMenuItem);

        MenuItemViewModel settingsMenuItem = CreateSettingsMenu();
        MenuItems.Add(settingsMenuItem);

        MenuItemViewModel toolsMenuItem = CreateToolsMenu();
        MenuItems.Add(toolsMenuItem);

        MenuItemViewModel windowMenuItem = new()
        {
            Header = "Window",
            Enabled = true,
            MenuItems =
            [
                new MenuItemViewModel()
                {
                    Header = "Reset Window Layout",
                    Enabled = true,
                    Command = new RelayCommand(OnResetWindowLayoutClicked),
                }
            ]
        };
        MenuItems.Add(windowMenuItem);
    }


    private MenuItemViewModel CreateFileMenu()
    {
        var file = new MenuItemViewModel()
        {
            Header = "File",
            Enabled = true,
        };
        file.MenuItems.Add(new MenuItemViewModel()
        {
            Header = "New",
            Enabled = true,
            IconKind = "Material.File",
            MenuItems =
            [
                new MenuItemViewModel()
                {
                    Header = "FSM (Finite State Machine)",
                    Command = new AsyncRelayCommand(OnNewFSMClicked),
                    IconKind = "Material.ChartTimelineVariant",
                    Enabled = true,
                },
                new MenuItemViewModel()
                {
                    Header = "Behavior Tree",
                    Command = new RelayCommand(OnNewBTClicked),
                    IconKind = "Material.Graph",
                    Enabled = false,
                }
            ]
        });

        file.MenuItems.Add(new MenuItemViewModel()
        {
            Header = "_Open File",
            Command = new AsyncRelayCommand(OnOpenFileClicked),
            IconKind = "Material.FileFind",
            HotKey = new Avalonia.Input.KeyGesture(Avalonia.Input.Key.O, modifiers: Avalonia.Input.KeyModifiers.Control),
            Enabled = true,
        });

        file.MenuItems.Add(MenuItemViewModel.Separator);

        _saveMenuItem = new MenuItemViewModel()
        {
            Header = $"Save",
            IconKind = "Material.ContentSave",
            Command = new AsyncRelayCommand<bool>(OnSave),
            Parameter = false,
            HotKey = new Avalonia.Input.KeyGesture(Avalonia.Input.Key.S, modifiers: Avalonia.Input.KeyModifiers.Control),
            Enabled = false,
        };
        file.MenuItems.Add(_saveMenuItem);
        _saveAsMenuItem = new MenuItemViewModel()
        {
            Header = $"Save As...",
            Command = new AsyncRelayCommand<bool>(OnSave),
            Parameter = true,
            Enabled = false,
        };
        file.MenuItems.Add(_saveAsMenuItem);
        file.MenuItems.Add(MenuItemViewModel.Separator);

        _recentItemsMenuItem = new MenuItemViewModel()
        {
            Header = "Recent Files",
            Enabled = true,
        };

        foreach (Uri recentFile in _recentFiles.GetRecentlyOpenedFiles())
        {
            AddRecentItem(recentFile);
        }

        file.MenuItems.Add(_recentItemsMenuItem);
        file.MenuItems.Add(MenuItemViewModel.Separator);

        file.MenuItems.Add(new MenuItemViewModel()
        {
            Header = "Exit",
            Command = new RelayCommand(OnExit),
            IconKind = "Material.ExitToApp",
            Enabled = true,
        });

        return file;
    }

    private void AddRecentItem(Uri recentFile)
    {
        _recentItemsMenuItem.MenuItems.Add(new MenuItemViewModel()
        {
            Header = $"_{recentFile.LocalPath}",
            Command = new RelayCommand<Uri>(OnRecentFileClicked),
            Parameter = recentFile,
            Enabled = true,
        });
    }

    private MenuItemViewModel CreateViewMenu()
    {
        var viewMenuItem = new MenuItemViewModel()
        {
            Header = "View",
            Enabled = true,
        };

        viewMenuItem.MenuItems.Add(new MenuItemViewModel()
        {
            Header = "Log Window",
            Enabled = true,
            IconKind = "Material.FormatListBulletedType",
            Command = new RelayCommand(View_OpenLogWindow),
            HotKey = new KeyGesture(Avalonia.Input.Key.L, Avalonia.Input.KeyModifiers.Control),
        });
        return viewMenuItem;
    }

    private void View_OpenLogWindow()
    {
        _dialog.OpenLogWindow();
    }

    private MenuItemViewModel CreateSettingsMenu()
    {
        var settingsMenuItem = new MenuItemViewModel()
        {
            Header = "Settings",
            Enabled = true,
        };

        var fsmOptions = App.Current.Services.GetRequiredService<IOptions<FSMOptions>>();
        settingsMenuItem.MenuItems.Add(new MenuItemViewModel()
        {
            Header = "Always expand FSM/BT conditions",
            Enabled = true,
            Checked = fsmOptions.Value.AlwaysExpandConditions,
            StaysOpenOnClick = true,
            ToggleType = MenuItemToggleType.CheckBox,
            Command = new RelayCommand(() =>
            {
                fsmOptions.Value.AlwaysExpandConditions = !fsmOptions.Value.AlwaysExpandConditions;
            })
        });

        settingsMenuItem.MenuItems.Add(new MenuItemViewModel()
        {
            Header = "Display FSM Guids",
            Enabled = true,
            Checked = fsmOptions.Value.DisplayFSMGuids,
            StaysOpenOnClick = true,
            ToggleType = MenuItemToggleType.CheckBox,
            Command = new RelayCommand(() =>
            {
                fsmOptions.Value.DisplayFSMGuids = !fsmOptions.Value.DisplayFSMGuids;
            })
        });

        settingsMenuItem.MenuItems.Add(new MenuItemViewModel()
        {
            Header = "Display Layer Indices",
            Enabled = true,
            Checked = fsmOptions.Value.DisplayLayerIndices,
            StaysOpenOnClick = true,
            ToggleType = MenuItemToggleType.CheckBox,
            Command = new RelayCommand(() =>
            {
                fsmOptions.Value.DisplayLayerIndices = !fsmOptions.Value.DisplayLayerIndices;
            })
        });

        return settingsMenuItem;
    }

    private MenuItemViewModel CreateToolsMenu()
    {
        var toolsMenuItem = new MenuItemViewModel()
        {
            Header = "Tools",
            Enabled = true,
        };

        var stringHasherMenuItem = new MenuItemViewModel()
        {
            Header = "String Hasher",
            Enabled = true,
            IconKind = "Material.Pound",
            Command = new AsyncRelayCommand(StringHasherClicked),
        };
        toolsMenuItem.MenuItems.Add(stringHasherMenuItem);
        toolsMenuItem.MenuItems.Add(MenuItemViewModel.Separator);

        var database = new MenuItemViewModel()
        {
            Header = "Database",
            Enabled = true,
            IconKind = "Material.Settings",
        };

        database.MenuItems.Add(new MenuItemViewModel()
        {
            Header = $"Reload FSM Component documentation ({ComponentDocumentationService.DOC_FILE_NAME})",
            Enabled = true,
            IconKind = "Material.FSM",
            Command = new RelayCommand(ReloadFSMDocumentation),
        });
        toolsMenuItem.MenuItems.Add(database);

        var scripts = new MenuItemViewModel()
        {
            Header = "Tests",
            Enabled = true,
            IconKind = "Material.Settings",
        };

        scripts.MenuItems.Add(new MenuItemViewModel()
        {
            Header = "Verify all Behavior Trees in folder",
            Enabled = true,
            IconKind = "Material.Settings",
            Command = new AsyncRelayCommand(VerifyAllBTs),
        });
        scripts.MenuItems.Add(new MenuItemViewModel()
        {
            Header = "Verify all FSMs in folder",
            Enabled = true,
            IconKind = "Material.Settings",
            Command = new AsyncRelayCommand(VerifyAllFSMs),
        });
        toolsMenuItem.MenuItems.Add(scripts);

        var themesMenuItem = new MenuItemViewModel()
        {
            Header = "Themes",
            Enabled = true,
            IconKind = "Material.ThemeLightDark",
        };
        toolsMenuItem.MenuItems.Add(themesMenuItem);

        foreach (var style in Enum.GetValues<AppTheme>())
        {
            var themeChangedCommand = new RelayCommand<AppTheme>(OnThemeChanged);
            var themeMenuItem = new MenuItemViewModel
            {
                Header = style == AppTheme.Default ? "System Default" : style.ToString(),
                IconKind = style != AppTheme.Default ? style == AppTheme.Light ? "Material.Brightness5" : "Material.Brightness2"
                                    : null,

                Command = themeChangedCommand,
                Parameter = style,
                Enabled = true,
                Checked = style == AppTheme.Default,
                ToggleType = MenuItemToggleType.Radio,
            };

            themesMenuItem.MenuItems.Add(themeMenuItem);
            _themeMenuItems.Add(themeMenuItem);
        }
        return toolsMenuItem;
    }

    public void ReloadFSMDocumentation()
    {
        try
        {
            var service = App.Current.Services.GetService<ComponentDocumentationService>();
            service.LoadDocumentFile();
            WeakReferenceMessenger.Default.Send(new SnackbarMessage($"Component documents updated. ({service.Components.Count} components)"));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to reload FSM documentation");
        }
    }

    public async Task VerifyAllBTs()
    {
        var folder = await _filesService.OpenFolderAsync("Select extracted system/behaviortree folder...");
        if (folder is null)
            return;

        string? localPath = folder.TryGetLocalPath();
        if (localPath is null)
            return;

        await _fsmBtVerifierService.VerifyAllBTs(localPath);

        WeakReferenceMessenger.Default.Send(new SnackbarMessage($"Done, check log file.", TimeSpan.FromSeconds(5)));
    }

    public async Task VerifyAllFSMs()
    {
        var folder = await _filesService.OpenFolderAsync("Select extracted system/fsm folder...");
        if (folder is null)
            return;

        string? localPath = folder.TryGetLocalPath();
        if (localPath is null)
            return;

        await _fsmBtVerifierService.VerifyAllFSMs(localPath);

        WeakReferenceMessenger.Default.Send(new SnackbarMessage($"Done, check log file.", TimeSpan.FromSeconds(5)));
    }

    public async Task StringHasherClicked()
    {
        await _dialog.ShowDialogAsync(new StringHasherViewModel());
    }

    public async Task OnOpenFileClicked()
    {
        var file = await _filesService.OpenFileAsync("Open file...", filters:
        [
            FilePickerFileTypes.All,
            new FilePickerFileType("FSM File")
            {
                Patterns = ["*_fsm_ingame.msg", "*_fsm_ingame.json"]
            },
            new FilePickerFileType("Generic Message File")
            {
                Patterns = ["*.msg", "*.json"]
            },
        ]);

        if (file is null) 
            return;

        using Stream stream = await file.OpenReadAsync();
        WeakReferenceMessenger.Default.Send(new FileOpenRequestMessage(new FileOpenResult(file.Path, stream)));
    }

    public async Task OnSave(bool isSaveAs)
    {
        var document = WeakReferenceMessenger.Default.Send(new GetCurrentDocumentRequest());
        if (!document.HasReceivedResponse || document.Response is null || document.Response is not ISaveableDocument saveableDocument)
            return;

        Uri? outputFile = await saveableDocument.SaveDocument(_filesService, isSaveAs);
        if (outputFile is not null)
        {
            _logger?.LogInformation("Saved as {outputFile}", outputFile);
            WeakReferenceMessenger.Default.Send(new SetStatusBarTextRequest($"{DateTime.Now} - Saved as {outputFile}."));
            WeakReferenceMessenger.Default.Send(new SnackbarMessage($"Saved as {outputFile}.", TimeSpan.FromSeconds(5)));

            _recentFiles.Add(outputFile);

            saveableDocument.LastFile = outputFile;
        }
    }

    public void OnRecentFileClicked(Uri fileUri)
    {
        if (!File.Exists(fileUri.LocalPath))
        {
            WeakReferenceMessenger.Default.Send(new SnackbarMessage($"File {fileUri.LocalPath} does not exist"));

            _recentFiles.Remove(fileUri);
            return;
        }

        using Stream stream = File.OpenRead(fileUri.LocalPath);
        WeakReferenceMessenger.Default.Send(new FileOpenRequestMessage(new FileOpenResult(fileUri, stream)));
    }

    public void OnExit()
    {
        if (Application.Current is not null && Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime classic)
        {
            classic.Shutdown();
        }
    }

    public async Task OnNewFSMClicked()
    {
        var mainView = App.Current.Services.GetRequiredService<MainViewModel>();
        await mainView.NewFSMDocument();
    }

    public void OnNewBTClicked()
    {
        var mainView = App.Current.Services.GetRequiredService<MainViewModel>();
        mainView.NewBTDocument();
    }

    public void OnThemeChanged(AppTheme parameter)
    {
        ThemeVariant themeVariant = parameter switch
        {
            AppTheme.Default => ThemeVariant.Default,
            AppTheme.Light => ThemeVariant.Light,
            AppTheme.Dark => ThemeVariant.Dark,
            _ => throw new InvalidOperationException($"Invalid theme {parameter}"),
        };

        if (Application.Current is not null && themeVariant != Application.Current.RequestedThemeVariant)
        {
            Application.Current.RequestedThemeVariant = themeVariant;

            foreach (var i in _themeMenuItems)
                i.Checked = (AppTheme)i.Parameter! == parameter;

            WeakReferenceMessenger.Default.Send(new ThemeChangedMessage(parameter));
        }
    }

    public void OnResetWindowLayoutClicked()
    {
        WeakReferenceMessenger.Default.Send(new DockLayoutResetRequest());
    }

}
