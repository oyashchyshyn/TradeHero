using Microsoft.Extensions.DependencyInjection;
using TradeHero.Database;

namespace TradeHero.Dependencies;

public static class ThDatabaseServiceCollectionExtensions
{
    public static void AddDatabase(this IServiceCollection serviceCollection)
    {
        DatabaseDiContainer.Register(serviceCollection);
    }
}