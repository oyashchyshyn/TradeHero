using Microsoft.Extensions.DependencyInjection;
using TradeHero.Strategies.Strategies.PercentMoveStrategy.Streams;

namespace TradeHero.Strategies.Strategies.PercentMoveStrategy.Factory;

internal class PmsSymbolTickerStreamFactory
{
    private readonly IServiceProvider _serviceProvider;
    
    public PmsSymbolTickerStreamFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public PmsSymbolTickerStream GetPmsSymbolTickerStream()
    {
        return _serviceProvider.GetRequiredService<PmsSymbolTickerStream>();
    }
}