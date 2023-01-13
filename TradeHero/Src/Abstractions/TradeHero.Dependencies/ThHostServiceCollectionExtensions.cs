using Microsoft.Extensions.DependencyInjection;
using TradeHero.Main;

namespace TradeHero.Dependencies;

public static class ThHostServiceCollectionExtensions
{
    public static void AddHost(this IServiceCollection serviceCollection)
    {
        HostDiContainer.Register(serviceCollection);
    }
}