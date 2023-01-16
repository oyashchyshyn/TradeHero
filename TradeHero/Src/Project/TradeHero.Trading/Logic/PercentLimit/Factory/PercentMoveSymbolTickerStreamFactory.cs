using Microsoft.Extensions.DependencyInjection;
using TradeHero.Trading.Logic.PercentLimit.Streams;

namespace TradeHero.Trading.Logic.PercentLimit.Factory;

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