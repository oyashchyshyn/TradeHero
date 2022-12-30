using Binance.Net.Enums;
using Binance.Net.Interfaces;
using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Client.Models.Response;

namespace TradeHero.Contracts.Client.CustomApi;

public interface IKlineApi
{
    Task<ThWebCallResult<List<IBinanceKline>>> GetKlineByDateRangeAsync(string symbolName, KlineInterval interval, DateTime startFrom, DateTime endTo, Market market, CancellationToken cancellationToken = default);
}