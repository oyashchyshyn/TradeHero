using Microsoft.Extensions.DependencyInjection;
using TradeHero.Trading;

namespace TradeHero.Dependencies;

public static class ThTradingServiceCollectionExtensions
{
    public static void AddTradingLogic(this IServiceCollection serviceCollection)
    {
        TradingDiContainer.Register(serviceCollection);
    }
}