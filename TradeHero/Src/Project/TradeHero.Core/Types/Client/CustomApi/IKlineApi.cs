using Binance.Net.Enums;
using Binance.Net.Interfaces;
using TradeHero.Core.Enums;
using TradeHero.Core.Types.Client.Models.Response;

namespace TradeHero.Core.Types.Client.CustomApi;

public interface IKlineApi
{
    Task<ThWebCallResult<List<IBinanceKline>>> GetKlineByDateRangeAsync(string symbolName, KlineInterval interval, DateTime startFrom, 
        DateTime endTo, Market market, CancellationToken cancellationToken = default);
}