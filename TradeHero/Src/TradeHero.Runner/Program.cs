using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TradeHero.Contracts.Services;
using TradeHero.DependencyResolver;
using TradeHero.Runner.Helpers;

namespace TradeHero.Runner;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        
        try
        {
            var environmentType = ArgumentsHelper.GetEnvironmentType(args);
            
            var host = Host.CreateDefaultBuilder(args)
                .UseEnvironment(environmentType.ToString())
                .UseContentRoot(baseDirectory)
                .ConfigureAppConfiguration((_, config) =>
                {
                    config.AddConfiguration(ConfigurationHelper.GenerateConfiguration(args));
                })
                .ConfigureServices((_, serviceCollection) =>
                {
                    serviceCollection.AddThDependencyCollection();
                })
                .Build();

            await host.Services.GetRequiredService<IStartupService>()
                .CheckIsFirstRunAsync();
            
            await host.RunAsync();
        }
        catch (Exception exception)
        {
            await ExceptionHelper.WriteExceptionAsync(exception, baseDirectory);
        }
    }
}