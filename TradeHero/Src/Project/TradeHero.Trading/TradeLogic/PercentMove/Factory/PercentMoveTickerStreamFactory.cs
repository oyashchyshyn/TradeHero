using Microsoft.Extensions.DependencyInjection;
using TradeHero.Trading.TradeLogic.PercentMove.Streams;

namespace TradeHero.Trading.TradeLogic.PercentMove.Factory;

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