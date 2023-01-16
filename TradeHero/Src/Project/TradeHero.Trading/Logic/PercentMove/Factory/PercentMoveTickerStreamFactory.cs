using Microsoft.Extensions.DependencyInjection;
using TradeHero.Trading.Logic.PercentMove.Streams;

namespace TradeHero.Trading.Logic.PercentMove.Factory;

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