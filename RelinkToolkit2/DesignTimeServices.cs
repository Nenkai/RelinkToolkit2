using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using RelinkToolkit2.Controls.PropertyGrid.ViewModel;
using RelinkToolkit2.ViewModels;
using RelinkToolkit2.ViewModels.Documents.GraphEditor;
using RelinkToolkit2.ViewModels.Windows;

using System;
using RelinkToolkit2.ViewModels.Tools;
using RelinkToolkit2.ViewModels.Tools.Properties;

namespace RelinkToolkit2;

public static class DesignTimeServices
{
    private static IServiceProvider? _provider;

    public static IServiceProvider Provider => _provider ??= BuildProvider();

    private static IServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));

        App.AddBaseServices(services, new ConfigurationBuilder().Build());

        return services.BuildServiceProvider();
    }
}

public static class DesignData
{
    public static MainViewModel MainViewModel => DesignTimeServices.Provider.GetRequiredService<MainViewModel>();
    public static DocumentablePropertyGridViewModel DocumentablePropertyGridViewModel => DesignTimeServices.Provider.GetRequiredService<DocumentablePropertyGridViewModel>();
    public static FsmEditorViewModel FsmEditorViewModel => DesignTimeServices.Provider.GetRequiredService<FsmEditorViewModel>();
    public static ConnectionEditorViewModel ConnectionEditorViewModel => DesignTimeServices.Provider.GetRequiredService<ConnectionEditorViewModel>();

    public static FsmNodePropertiesEditorViewModel FsmNodePropertiesEditorViewModel => DesignTimeServices.Provider.GetRequiredService<FsmNodePropertiesEditorViewModel>();
    public static BtNodePropertiesEditorViewModel BtNodePropertiesEditorViewModel => DesignTimeServices.Provider.GetRequiredService<BtNodePropertiesEditorViewModel>();
}
