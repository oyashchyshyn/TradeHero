using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TradeHero.Contracts.Base.Enums;
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
            var environmentType = args.Any() 
                ? (EnvironmentType)Enum.Parse(typeof(EnvironmentType), args[0]) 
                : EnvironmentType.Production;
            
            if (await FirstRunScreen.RunAsync(environmentType, baseDirectory))
            {
                var host = Host.CreateDefaultBuilder(args)
                    .UseEnvironment(environmentType.ToString())
                    .UseContentRoot(baseDirectory)
                    .ConfigureServices((_, serviceCollection) =>
                    {
                        serviceCollection.AddThDependencyCollection();
                        serviceCollection.AddSingleton<IHostLifetime, TradeHeroLifetime>();
                    })
                    .Build();

                await host.RunAsync();   
            }
        }
        catch (Exception exception)
        {
            await ExceptionHelper.WriteExceptionAsync(exception, baseDirectory);
        }
    }
}