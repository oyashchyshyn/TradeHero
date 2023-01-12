using Microsoft.Extensions.DependencyInjection;
using TradeHero.Client;

namespace TradeHero.Dependencies;

public static class ThClientServiceCollectionExtensions
{
    public static void AddClient(this IServiceCollection serviceCollection)
    {
        ClientDiContainer.Register(serviceCollection);
    }
}