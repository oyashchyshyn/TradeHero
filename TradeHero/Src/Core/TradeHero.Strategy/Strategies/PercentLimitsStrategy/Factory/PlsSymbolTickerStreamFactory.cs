using Microsoft.Extensions.DependencyInjection;
using TradeHero.Strategies.Strategies.PercentLimitsStrategy.Streams;

namespace TradeHero.Strategies.Strategies.PercentLimitsStrategy.Factory;

internal class PlsSymbolTickerStreamFactory
{
    private readonly IServiceProvider _serviceProvider;
    
    public PlsSymbolTickerStreamFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public PlsSymbolTickerStream GetPlsSymbolTickerStream()
    {
        return _serviceProvider.GetRequiredService<PlsSymbolTickerStream>();
    }
}