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
using Avalonia.Platform.Storage;
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

namespace RelinkToolkit2.ViewModels;

public partial class TopMenuViewModel : ObservableObject
{
    public ObservableCollection<IMenuItemViewModel> MenuItems { get; set; } = [];
    public ObservableCollection<MenuItemViewModel> _themeMenuItems = [];

    private MenuItemViewModel? _menuItem_SaveCurrentFSMGraph;

    public TopMenuViewModel() 
    {
        BuildMenu();
    }

    public void BuildMenu()
    {
        MenuItemViewModel fileMenuItem = CreateFileMenu();
        MenuItems.Add(fileMenuItem);

        MenuItemViewModel viewMenuItem = CreateViewMenu();
        MenuItems.Add(viewMenuItem);

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

        

        WeakReferenceMessenger.Default.Register<FSMFileLoadStateChangedMessage>(this, (recipient, message)
            => _menuItem_SaveCurrentFSMGraph.Enabled = message.Value);
    }

    private MenuItemViewModel CreateFileMenu()
    {
        _menuItem_SaveCurrentFSMGraph = new MenuItemViewModel()
        {
            Header = "Current FSM Graph",
            Command = new RelayCommand(OnSaveGraph),
            IconKind = "Material.Graph",
            Checked = false,
        };

        return new MenuItemViewModel()
        {
            Header = "File",
            Enabled = true,
            MenuItems = [new MenuItemViewModel()
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
            },
            new MenuItemViewModel()
            {
                Header = "Open File",
                Command = new RelayCommand(OnOpenFileClicked),
                IconKind = "Material.FileFind",
                Enabled = true,
            },
            MenuItemViewModel.Separator,
            new MenuItemViewModel()
            {
                Header = "Save",
                IconKind = "Material.ContentSave",
                MenuItems = [_menuItem_SaveCurrentFSMGraph],
                Enabled = true,
            },
            MenuItemViewModel.Separator,
            new MenuItemViewModel()
            {
                Header = "Exit",
                Command = new RelayCommand(OnExit),
                IconKind = "Material.ExitToApp",
                Enabled = true,
            }]
        };
    }

    private MenuItemViewModel CreateViewMenu()
    {
        var viewMenuItem = new MenuItemViewModel()
        {
            Header = "View",
            Enabled = true,
        };

        var themesMenuItem = new MenuItemViewModel()
        {
            Header = "Themes",
            Enabled = true,
            IconKind = "Material.ThemeLightDark",
        };
        viewMenuItem.MenuItems.Add(themesMenuItem);

        foreach (var style in Enum.GetValues<AppTheme>())
        {
            var themeChangedCommand = new RelayCommand<AppTheme>(OnThemeChanged);
            var themeMenuItem = new MenuItemViewModel
            {
                Header = style.ToString(),
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

        return viewMenuItem;
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

    public async void OnSaveGraph()
    {
        var filesService = App.Current?.Services?.GetService<IFilesService>();
        if (filesService is not null)
        {
            var file = await filesService.SaveFileAsync("Save FSM file", "JSON Files|*.json|" +
                              "MessagePack|*.msg",
                              "fsm.json");
            if (file is null)
                return;

            WeakReferenceMessenger.Default.Send(new GraphFileSaveRequestMessage(file.Path.LocalPath));
        }
        else
        {
            throw new Exception("Could not fetch IFilesService");
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
