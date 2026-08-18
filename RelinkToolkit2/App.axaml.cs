using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.PropertyGrid.Services;
using Avalonia.Styling;

using GBFRDataTools.FSM;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NLog.Extensions.Logging;

using Nodify;

using PropertyModels.Localization;

using RelinkToolkit2.Controls.PropertyGrid;
using RelinkToolkit2.Logging;
using RelinkToolkit2.Services;
using RelinkToolkit2.ViewModels;
using RelinkToolkit2.ViewModels.Documents;
using RelinkToolkit2.Views;

using System;
using System.ComponentModel;
using System.IO;


namespace RelinkToolkit2;

public partial class App : Application
{
    public new static App Current => (App)Application.Current!;

    public IHost Host { get; private set; } = null!;
    public IServiceProvider? Services => Host.Services;

    public Window? MainWindow { get; private set; }

    public override void Initialize()
    {
        CellEditFactoryService.Default.AddFactory(new eObjIdCellEditFactory());
        CellEditFactoryService.Default.AddFactory(new Vector4CellEditFactory());
        CellEditFactoryService.Default.AddFactory(new Vector3CellEditFactory());
        CellEditFactoryService.Default.AddFactory(new Vector2CellEditFactory());

        NodifyEditor.EnableSnappingCorrection = false;

        AvaloniaXamlLoader.Load(this);

#if DEBUG
        this.AttachDeveloperTools();
#endif
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();

        builder.Services.AddLogging(e =>
        {
            e.AddProvider(new LogControlLoggerProvider(new LogControlLoggerConfiguration() { LogLevel = LogLevel.Trace }));
            e.AddProvider(new NLogLoggerProvider());
        });

        AddBaseServices(builder.Services);

        ILogger? logger = null;
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            builder.Services.AddSingleton<IFilesService>(x => new FilesService(desktop.MainWindow.StorageProvider));
            Host = builder.Build();

            logger = Services.GetRequiredService<ILogger<App>>();
            var vm = Services.GetRequiredService<MainViewModel>();

            desktop.MainWindow = new MainWindow
            {
                DataContext = vm,
            };
            MainWindow = desktop.MainWindow;
            Services.GetRequiredService<DialogService>().SetMainWindow(desktop.MainWindow);
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            builder.Services.AddSingleton<IFilesService>(x => new FilesService());
            Host = builder.Build();

            logger = Host.Services.GetRequiredService<ILogger>();

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
        
        logger?.LogInformation("Services initialized.");
        base.OnFrameworkInitializationCompleted();
    }

    public static void AddBaseServices(IServiceCollection services)
    {
        services.AddSingleton<DialogService>();

        services.AddSingleton<ComponentDocumentationService>();
        services.AddSingleton<ComponentSearchService>();
        services.AddHostedService<ComponentDocumentationService>(sp => {
            return sp.GetRequiredService<ComponentDocumentationService>();
        });

        services.AddSingleton<SolutionExplorerViewModel>();
        services.AddSingleton<PropertyGridViewModel>();
        services.AddSingleton<ConnectionEditorViewModel>();
        services.AddSingleton<ToolboxViewModel>();
        services.AddSingleton<DocumentsViewModel>();
        services.AddSingleton<DockFactory>();
        services.AddSingleton<TopMenuViewModel>();
        services.AddSingleton<StatusBarViewModel>();
        services.AddSingleton<MainViewModel>();
    }
}
