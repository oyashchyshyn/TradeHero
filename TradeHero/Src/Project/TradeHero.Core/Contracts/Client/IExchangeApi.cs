using Binance.Net.Enums;
using TradeHero.Core.Enums;
using TradeHero.Core.Models.Client;

namespace TradeHero.Core.Contracts.Client;

public interface IExchangeApi
{
    public Task<ThWebCallResult<BinanceKlineVolatility>> GetVolatilityAsync(string symbolName, KlineInterval interval, int klinesBack, 
        Market market, CancellationToken cancellationToken = default);
}