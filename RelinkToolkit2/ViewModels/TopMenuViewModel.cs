using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.Controls.ApplicationLifetimes;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using RelinkToolkit2.Messages;
using RelinkToolkit2.Messages.IO;
using RelinkToolkit2.Services;

using RelinkToolkit2.ViewModels.Documents;
using RelinkToolkit2.ViewModels.Menu;
using RelinkToolkit2.Messages.Fsm;
using System.Diagnostics.CodeAnalysis;
using RelinkToolkit2.Messages.Documents;
using RelinkToolkit2.Messages.Dialogs;
using RelinkToolkit2.Messages.StatusBar;
using RelinkToolkit2.ViewModels.Documents.Interfaces;
using RelinkToolkit2.Views.Tools;
using Microsoft.Extensions.Logging;

namespace RelinkToolkit2.ViewModels;

public partial class TopMenuViewModel : ObservableObject
{
    private ILogger? _logger;

    public ObservableCollection<IMenuItemViewModel> MenuItems { get; set; } = [];
    public ObservableCollection<MenuItemViewModel> _themeMenuItems = [];

    private MenuItemViewModel _saveMenuItem;
    private MenuItemViewModel _saveAsMenuItem;

    public TopMenuViewModel(ILogger<TopMenuViewModel>? logger) 
    {
        _logger = logger;

        BuildMenu();

        WeakReferenceMessenger.Default.Register<ActiveDocumentChangedMessage>(this, (recipient, message) =>
        {
            EditorDocumentBase? doc = message.Value;
            bool isSaveable = doc is ISaveableDocument;
            _saveMenuItem!.Enabled = isSaveable;
            _saveAsMenuItem!.Enabled = isSaveable;

            if (isSaveable)
                _saveAsMenuItem!.Header = $"Save {doc!.Title} As...";
            else
                _saveAsMenuItem!.Header = $"Save As...";
        });
    }

    public void BuildMenu()
    {
        MenuItemViewModel fileMenuItem = CreateFileMenu();
        MenuItems.Add(fileMenuItem);

        MenuItemViewModel viewMenuItem = CreateViewMenu();
        MenuItems.Add(viewMenuItem);

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
            MenuItems = [new MenuItemViewModel()
            {
                Header = "Quest",
                Command = new RelayCommand(OnNewQuestClicked),
                IconKind = "Material.Script",
                Enabled = true,
            }]
        });

        file.MenuItems.Add(new MenuItemViewModel()
        {
            Header = "Open File",
            Command = new RelayCommand(OnOpenFileClicked),
            IconKind = "Material.FileFind",
            HotKey = new Avalonia.Input.KeyGesture(Avalonia.Input.Key.O, modifiers: Avalonia.Input.KeyModifiers.Control),
            Enabled = true,
        });

        file.MenuItems.Add(MenuItemViewModel.Separator);

        _saveMenuItem = new MenuItemViewModel()
        {
            Header = $"Save",
            IconKind = "Material.ContentSave",
            Command = new RelayCommand<bool>(OnSave),
            Parameter = false,
            HotKey = new Avalonia.Input.KeyGesture(Avalonia.Input.Key.S, modifiers: Avalonia.Input.KeyModifiers.Control),
            Enabled = false,
        };
        file.MenuItems.Add(_saveMenuItem);
        _saveAsMenuItem = new MenuItemViewModel()
        {
            Header = $"Save As...",
            Command = new RelayCommand<bool>(OnSave),
            Parameter = true,
            Enabled = false,
        };
        file.MenuItems.Add(_saveAsMenuItem);
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
        var logWindow = new LogWindow();
        logWindow.Show();
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
            Command = new RelayCommand(StringHasherClicked),
        };
        toolsMenuItem.MenuItems.Add(stringHasherMenuItem);
        toolsMenuItem.MenuItems.Add(MenuItemViewModel.Separator);

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

    public void StringHasherClicked()
    {
        var window = new StringHasherWindow();
        window.Show();
    }

    public async void OnOpenFileClicked()
    {
        var filesService = App.Current?.Services?.GetService<IFilesService>();
        if (filesService is null)
            return;

        var file = await filesService.OpenFileAsync("Open FSM file", "");
        if (file is null) 
            return;

        var stream = await file.OpenReadAsync();
        WeakReferenceMessenger.Default.Send(new FileOpenRequestMessage(new FileOpenResult(file.Path, stream)));
    }

    public async void OnSave(bool isSaveAs)
    {
        var document = WeakReferenceMessenger.Default.Send(new GetCurrentDocumentRequest());
        if (!document.HasReceivedResponse || document.Response is null || document.Response is not ISaveableDocument saveableDocument)
            return;

        var filesService = (App.Current?.Services?.GetService<IFilesService>()) ?? throw new Exception("Could not fetch IFilesService");
        string? outputFile = await saveableDocument.SaveDocument(filesService, isSaveAs);
        if (!string.IsNullOrEmpty(outputFile))
        {
            _logger?.LogInformation("Saved file as {outputFile}", outputFile);
            WeakReferenceMessenger.Default.Send(new SetStatusBarTextRequest($"{DateTime.Now} - Saved file as {outputFile}."));
        }
    }

    public void OnExit()
    {
        if (Application.Current is not null && Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime classic)
        {
            classic.Shutdown();
        }
    }

    public void OnNewQuestClicked()
    {

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
