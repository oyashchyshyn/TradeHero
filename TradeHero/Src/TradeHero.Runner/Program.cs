using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TradeHero.DependencyResolver;
using TradeHero.Runner.Helpers;

namespace TradeHero.Runner;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        try
        {
            var host = Host.CreateDefaultBuilder(args)
                .UseEnvironment(args.Any() ? args[0] : "Production")
                .UseContentRoot(AppDomain.CurrentDomain.BaseDirectory)
                .ConfigureServices((_, serviceCollection) =>
                {
                    serviceCollection.AddThDependencyCollection();
                    serviceCollection.AddSingleton<IHostLifetime, TradeHeroLifetime>();
                })
                .Build();

            await host.RunAsync();
        }
        catch (Exception exception)
        {
            await ExceptionHelper.WriteExceptionAsync(exception);
        }
    }
}