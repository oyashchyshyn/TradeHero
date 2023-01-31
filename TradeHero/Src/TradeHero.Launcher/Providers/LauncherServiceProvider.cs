using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TradeHero.Core.Contracts.Settings;
using TradeHero.Core.Extensions;
using TradeHero.Database;
using TradeHero.Launcher.Services;
using TradeHero.Services;

namespace TradeHero.Launcher.Providers;

public static class LauncherServiceProvider
{
    public static ServiceProvider Build(AppSettings appSettings)
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddSingleton<LauncherStartupService>();

        serviceCollection.AddDatabase();
        serviceCollection.AddServices(appSettings);
        
        serviceCollection.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddThSerilog();
        });
        
        return serviceCollection.BuildServiceProvider(true);
    }
}