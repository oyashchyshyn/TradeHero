using Microsoft.Extensions.DependencyInjection;
using TradeHero.Client;
using TradeHero.Core;
using TradeHero.Database;
using TradeHero.EntryPoint;
using TradeHero.Strategies;

namespace TradeHero.DependencyResolver;

public static class ThDependencyResolverCollectionExtension
{
    public static void AddThDependencyCollection(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddThCore();
        serviceCollection.AddThClient();
        serviceCollection.AddThDatabase();
        serviceCollection.AddThDatabase();
        serviceCollection.AddThStrategy();
        serviceCollection.AddThLogic();
    }
}