using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using RelinkToolkit2.Services;
using RelinkToolkit2.ViewModels;
using RelinkToolkit2.ViewModels.Documents;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RelinkToolkit2;

public static class DesignTimeServices
{
    private static IServiceProvider? _provider;

    public static IServiceProvider Provider => _provider ??= BuildProvider();

    private static IServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));

        App.AddBaseServices(services);

        return services.BuildServiceProvider();
    }
}

public static class DesignData
{
    public static MainViewModel MainViewModel => DesignTimeServices.Provider.GetRequiredService<MainViewModel>();
    public static PropertyGridViewModel PropertyGridViewModel => DesignTimeServices.Provider.GetRequiredService<PropertyGridViewModel>();
}
