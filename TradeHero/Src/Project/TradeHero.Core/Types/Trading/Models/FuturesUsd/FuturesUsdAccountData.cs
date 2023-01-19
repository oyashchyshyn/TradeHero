using Binance.Net.Objects.Models.Futures;

namespace TradeHero.Core.Types.Trading.Models.FuturesUsd;

public class FuturesUsdAccountData
{
    public IEnumerable<BinanceFuturesAccountBalance> Balances { get; set; } = Enumerable.Empty<BinanceFuturesAccountBalance>();
    public IEnumerable<BinancePositionDetailsUsdt> Positions { get; set; } = Enumerable.Empty<BinancePositionDetailsUsdt>();
}