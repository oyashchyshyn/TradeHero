using Microsoft.Extensions.DependencyInjection;
using TradeHero.StrategyRunner.TradeLogic.PercentMove.Streams;

namespace TradeHero.StrategyRunner.TradeLogic.PercentMove.Factory;

internal class PercentMoveTickerStreamFactory
{
    private readonly IServiceProvider _serviceProvider;
    
    public PercentMoveTickerStreamFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public PercentMoveSymbolTickerStream GetPmsSymbolTickerStream()
    {
        return _serviceProvider.GetRequiredService<PercentMoveSymbolTickerStream>();
    }
}