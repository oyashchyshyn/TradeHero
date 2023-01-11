using Microsoft.Extensions.DependencyInjection;
using TradeHero.Client;
using TradeHero.Database;
using TradeHero.Host;
using TradeHero.Services;
using TradeHero.StrategyRunner;

namespace TradeHero.DependencyResolver;

public static class ThDependencyResolverCollectionExtension
{
    public static void AddThDependencyCollection(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddThServices();
        serviceCollection.AddThClient();
        serviceCollection.AddThDatabase();
        serviceCollection.AddThDatabase();
        serviceCollection.AddThStrategyRunner();
        serviceCollection.AddThHost();
    }
}