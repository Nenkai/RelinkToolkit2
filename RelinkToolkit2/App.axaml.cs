﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

using Microsoft.Extensions.DependencyInjection;

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


    public override void Initialize()
    {
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
        services.AddSingleton<MainViewModel>();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow is not null)
        {
            services.AddSingleton<IFilesService>(x => new FilesService(desktop.MainWindow.StorageProvider));
            Services = services.BuildServiceProvider();

            var vm = Services.GetRequiredService<MainViewModel>();

            desktop.MainWindow = new MainWindow
            {
                DataContext = vm,
            };

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
