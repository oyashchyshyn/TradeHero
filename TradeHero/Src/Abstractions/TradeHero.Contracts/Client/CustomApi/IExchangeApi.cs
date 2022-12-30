using Binance.Net.Enums;
using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Client.Models;
using TradeHero.Contracts.Client.Models.Response;

namespace TradeHero.Contracts.Client.CustomApi;

public interface IExchangeApi
{
    public Task<ThWebCallResult<BinanceKlineVolatility>> GetVolatilityAsync(string symbolName, KlineInterval interval, int klinesBack, Market market, CancellationToken cancellationToken = default);
}