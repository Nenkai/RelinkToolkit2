using Aldwych.Logging;
using Aldwych.Logging.ViewModels;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.PropertyGrid.Services;
using Avalonia.Styling;

using DynamicData.Binding;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Nodify;

using ReactiveUI;

using RelinkToolkit2.Controls.PropertyGrid;
using RelinkToolkit2.Services;
using RelinkToolkit2.ViewModels;
using RelinkToolkit2.ViewModels.Documents;
using RelinkToolkit2.Views;

using System;

namespace RelinkToolkit2;

public partial class App : Application
{
    public new static App? Current => Application.Current as App;

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> instance to resolve application services.
    /// </summary>
    public IServiceProvider? Services { get; private set; }

    public Window? MainWindow { get; private set; }

    public override void Initialize()
    {
        CellEditFactoryService.Default.AddFactory(new eObjIdCellEditFactory());
        CellEditFactoryService.Default.AddFactory(new Vector4CellEditFactory());
        CellEditFactoryService.Default.AddFactory(new Vector3CellEditFactory());
        CellEditFactoryService.Default.AddFactory(new Vector2CellEditFactory());

        NodifyEditor.EnableSnappingCorrection = false;

        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {

        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        var services = new ServiceCollection();
        services.AddSingleton<SolutionExplorerViewModel>();
        services.AddSingleton<PropertyGridViewModel>();
        services.AddSingleton<ConnectionEditorViewModel>();
        services.AddSingleton<ToolboxViewModel>();
        services.AddSingleton<DocumentsViewModel>();
        services.AddSingleton<DockFactory>();
        services.AddSingleton<TopMenuViewModel>();
        services.AddSingleton<StatusBarViewModel>();
        services.AddSingleton<MainViewModel>();
        services.AddLogging(e =>
        {
            e.AddProvider(new LogControlLoggerProvider(new LogControlLoggerConfiguration() { LogLevel = LogLevel.Trace }));
        });

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            services.AddSingleton<IFilesService>(x => new FilesService(desktop.MainWindow.StorageProvider));
            Services = services.BuildServiceProvider();

            var vm = Services.GetRequiredService<MainViewModel>();

            desktop.MainWindow = new MainWindow
            {
                DataContext = vm,
            };
            MainWindow = desktop.MainWindow;

        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            services.AddSingleton<IFilesService>(x => new FilesService());
            Services = services.BuildServiceProvider();

            var vm = Services.GetRequiredService<MainViewModel>();
            singleViewPlatform.MainView = new MainView()
            {
                DataContext = vm,
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
