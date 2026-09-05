using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.PropertyGrid.Services;

using Dock.Controls.DeferredContentControl;
using Dock.Model;
using Dock.Model.Controls;
using Dock.Model.Core;

using GBFRDataTools.FSM;

/* using Dock.Serializer; */

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NLog.Extensions.Logging;
using NLog.Layouts;

using Nodify;

using RelinkToolkit2.Controls.PropertyGrid;
using RelinkToolkit2.Controls.PropertyGrid.ViewModel;
using RelinkToolkit2.Logging;
using RelinkToolkit2.Services;
using RelinkToolkit2.Services.Configuration;
using RelinkToolkit2.Services.Configuration.Models;
using RelinkToolkit2.Services.FSM;
using RelinkToolkit2.ViewModels;
using RelinkToolkit2.ViewModels.Documents;
using RelinkToolkit2.ViewModels.Documents.GraphEditor;
using RelinkToolkit2.ViewModels.Tools;
using RelinkToolkit2.ViewModels.Tools.Properties;
using RelinkToolkit2.ViewModels.Windows;
using RelinkToolkit2.Views;
using RelinkToolkit2.Views.Windows;

using System;
using System.IO;

namespace RelinkToolkit2;

public partial class App : Application
{
    public new static App Current => (App)Application.Current!;

    public IHost Host { get; private set; } = null!;

    public IServiceProvider? Services => Host.Services;
    public ConfigurationService? Configuration => Services?.GetRequiredService<ConfigurationService>();

    public Window? MainWindow { get; private set; }

    private ILogger? _logger;

    public override void Initialize()
    {
        // Property grid factories.
        CellEditFactoryService.Default.AddFactory(new eObjIdCellEditFactory());
        CellEditFactoryService.Default.AddFactory(new Vector4CellEditFactory());
        CellEditFactoryService.Default.AddFactory(new Vector3CellEditFactory());
        CellEditFactoryService.Default.AddFactory(new Vector2CellEditFactory());

        // Nodify settings.
        NodifyEditor.EnableSnappingCorrection = false;

        // Dock System: Make reveal duration for deferred documents zero.
        // Why: Because FSM documents would appear with 85% opacity, the timer wouldn't play for some reason
        // https://github.com/wieslawsoltes/Dock/blob/72fe7d19ab91748d8d0778817d065d1d59f8ec34/src/Dock.Controls.DeferredContentControl/DeferredContentControl.cs#L1076
        // It would make the document subtlely transparent, but on light theme it'd make the background/grid not visible
        DeferredContentPresentationSettings.RevealDuration = TimeSpan.Zero;

        AvaloniaXamlLoader.Load(this);
#if DEBUG
        this.AttachDeveloperTools();
#endif
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();

        string configPath = ConfigurationService.DefaultPath;
        if (!File.Exists(configPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
            File.WriteAllText(configPath, "{}");
        }

        builder.Configuration.AddJsonFile(configPath).Build();
        builder.Services.AddLogging(e =>
        {
            e.AddProvider(new LogControlLoggerProvider(new LogControlLoggerConfiguration() { LogLevel = LogLevel.Trace }));
            e.AddProvider(new NLogLoggerProvider());
        });

        AddBaseServices(builder.Services, builder.Configuration);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Host = builder.Build();

            desktop.Exit += Desktop_Exit;
            _logger = Services.GetRequiredService<ILogger<App>>();
            var vm = Services.GetRequiredService<MainViewModel>();

            desktop.MainWindow = new MainWindow
            {
                DataContext = vm,
            };
            MainWindow = desktop.MainWindow;
            Services.GetRequiredService<IDialogService>().SetMainWindow(desktop.MainWindow);
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            builder.Services.AddSingleton<IFilesService>(x => new FilesService());
            Host = builder.Build();

            _logger = Host.Services.GetRequiredService<ILogger>();

            var vm = Host.Services.GetRequiredService<MainViewModel>();
            singleViewPlatform.MainView = new MainView()
            {
                DataContext = vm,
            };
        }
        else
        {
            Host = builder.Build();
        }

        await Host.StartAsync();

        _logger?.LogInformation("Services initialized.");
        _logger?.LogInformation("{} FSM/BT components are currently supported.", FSMParser.ComponentNameToType.Count);

        base.OnFrameworkInitializationCompleted();
    }

    private void Desktop_Exit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        _logger?.LogInformation("Exit called..");

        _logger?.LogInformation("Saving configuration.");
        Configuration?.Save();

        SaveDockLayout();

        _logger?.LogInformation("Saving component documentation.");
        var docService = App.Current.Services.GetRequiredService<ComponentDocumentationService>();
        docService.SaveDocumentFile();

        _logger?.LogInformation("All saved. Exiting.");
    }

    private void SaveDockLayout()
    {
        DockFactory? dock = Services?.GetService<DockFactory>();
        DockState? dockState = App.Current.Services?.GetService<DockState>();

        /* TODO: Figure out why serialization doesn't work, what I am doing wrong with the library or MVVM model
         * Reintroduce the serializer nuget package if needed
        if (dock is not null && dockState is not null)
        {
            IRootDock? rootDock = Services?.GetService<MainViewModel>()?.Layout;
            if (rootDock is not null)
            {
                dockState?.Save(rootDock);

                Directory.CreateDirectory("config");

                using FileStream stream = File.Create(DockFactory.LayoutFilePath);

                var serializer = new DockSerializer();
                serializer.Save(stream, rootDock);
            }
        }
        */
    }

    private static IStorageProvider? GetStorageProvider()
    {
        var result = Application.Current?.ApplicationLifetime switch
        {
            IClassicDesktopStyleApplicationLifetime d => d.MainWindow,
            ISingleViewApplicationLifetime s => s.MainView as TopLevel,
            _ => null
        };
        return result?.StorageProvider;
    }

    public static void AddBaseServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IFilesService>(x => new FilesService(GetStorageProvider()));

        services.Configure<RootConfig>(configuration);
        services.AddSingleton(sp => Options.Create(sp.GetRequiredService<IOptions<RootConfig>>().Value.FileHistory));
        services.AddSingleton(sp => Options.Create(sp.GetRequiredService<IOptions<RootConfig>>().Value.FSMOptions));

        services.AddSingleton<ConfigurationService>();

        services.AddSingleton<IDialogService, DialogService>();

        services.AddSingleton<RecentFilesService>();
        services.AddSingleton<FSMFileService>();

        services.AddSingleton<ComponentDocumentationService>();
        services.AddSingleton<ComponentSearchService>();
        services.AddHostedService(sp => {
            return sp.GetRequiredService<ComponentDocumentationService>();
        });

        services.AddSingleton<DockFactory>();
        services.AddSingleton<DockState>();
        //services.AddSingleton<IDockSerializer>(static sp => new DockSerializer(sp));

        // Documents
        services.AddSingleton<DocumentsViewModel>();
        services.AddTransient<GenericEntityEditorViewModel>();
        services.AddTransient<FsmEditorViewModel>();
        services.AddTransient<BTEditorViewModel>();
        services.AddTransient<DocumentablePropertyGridViewModel>();

        // Tools
        services.AddSingleton<SolutionExplorerViewModel>();
        services.AddSingleton<ConnectionEditorViewModel>();
        services.AddSingleton<ToolboxViewModel>();

        services.AddSingleton<FsmNodePropertiesEditorViewModel>();
        services.AddSingleton<BtNodePropertiesEditorViewModel>();

        // Main
        services.AddSingleton<TopMenuViewModel>();
        services.AddSingleton<StatusBarViewModel>();
        services.AddSingleton<MainViewModel>();
    }
}
