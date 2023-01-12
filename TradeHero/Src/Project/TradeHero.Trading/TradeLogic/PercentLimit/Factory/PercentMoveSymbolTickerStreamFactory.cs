using Microsoft.Extensions.DependencyInjection;
using TradeHero.Trading.TradeLogic.PercentLimit.Streams;

namespace TradeHero.Trading.TradeLogic.PercentLimit.Factory;

internal class PercentMoveSymbolTickerStreamFactory
{
    private readonly IServiceProvider _serviceProvider;
    
    public PercentMoveSymbolTickerStreamFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public PercentLimitSymbolTickerStream GetPlsSymbolTickerStream()
    {
        return _serviceProvider.GetRequiredService<PercentLimitSymbolTickerStream>();
    }
}