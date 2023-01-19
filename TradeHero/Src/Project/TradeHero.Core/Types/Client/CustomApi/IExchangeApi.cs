using Binance.Net.Enums;
using TradeHero.Core.Enums;
using TradeHero.Core.Types.Client.Models;
using TradeHero.Core.Types.Client.Models.Response;

namespace TradeHero.Core.Types.Client.CustomApi;

public interface IExchangeApi
{
    public Task<ThWebCallResult<BinanceKlineVolatility>> GetVolatilityAsync(string symbolName, KlineInterval interval, int klinesBack, 
        Market market, CancellationToken cancellationToken = default);
}