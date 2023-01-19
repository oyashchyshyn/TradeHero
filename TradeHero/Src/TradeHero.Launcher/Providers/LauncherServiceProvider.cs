using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradeHero.Core.Enums;
using TradeHero.Core.Extensions;
using TradeHero.Database;
using TradeHero.Launcher.Services;
using TradeHero.Services;

namespace TradeHero.Launcher.Providers;

public static class LauncherServiceProvider
{
    public static ServiceProvider Build(IConfiguration configuration, string basePath, EnvironmentType environmentType)
    {
        var serviceCollection = new ServiceCollection();
        
        serviceCollection.AddSingleton<IConfiguration>(_ => configuration);

        serviceCollection.AddSingleton<IHostEnvironment, LauncherHostEnvironment>(_ => 
            new LauncherHostEnvironment(environmentType.ToString(), string.Empty, basePath, new NullFileProvider()));
        
        serviceCollection.AddSingleton<IHostApplicationLifetime, LauncherHostLifetime>(_ => 
            new LauncherHostLifetime(CancellationToken.None, CancellationToken.None, CancellationToken.None));

        serviceCollection.AddSingleton<LauncherStartupService>();

        serviceCollection.AddDatabase();
        serviceCollection.AddServices();
        
        serviceCollection.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddThSerilog();
        });
        
        return serviceCollection.BuildServiceProvider(true);
    }
}