using Microsoft.Extensions.DependencyInjection;
using TradeHero.Services;

namespace TradeHero.Dependencies;

public static class ThServiceServiceCollectionExtensions
{
    public static void AddServices(this IServiceCollection serviceCollection)
    {
        ServicesDiContainer.Register(serviceCollection);
    }
}