using Binance.Net.Enums;
using Binance.Net.Interfaces;
using TradeHero.Core.Enums;
using TradeHero.Core.Models.Client;

namespace TradeHero.Core.Contracts.Client;

public interface IKlineApi
{
    Task<ThWebCallResult<List<IBinanceKline>>> GetKlineByDateRangeAsync(string symbolName, KlineInterval interval, DateTime startFrom, 
        DateTime endTo, Market market, CancellationToken cancellationToken = default);
}