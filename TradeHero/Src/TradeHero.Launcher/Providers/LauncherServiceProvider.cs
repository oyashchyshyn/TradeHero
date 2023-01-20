using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TradeHero.Core.Extensions;
using TradeHero.Database;
using TradeHero.Launcher.Services;
using TradeHero.Services;

namespace TradeHero.Launcher.Providers;

public static class LauncherServiceProvider
{
    public static ServiceProvider Build(IConfiguration configuration, CancellationTokenSource cancellationTokenSource)
    {
        var serviceCollection = new ServiceCollection();
        
        serviceCollection.AddSingleton<IConfiguration>(_ => configuration);

        serviceCollection.AddSingleton<LauncherStartupService>();

        serviceCollection.AddDatabase();
        serviceCollection.AddServices(cancellationTokenSource);
        
        serviceCollection.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddThSerilog();
        });
        
        return serviceCollection.BuildServiceProvider(true);
    }
}