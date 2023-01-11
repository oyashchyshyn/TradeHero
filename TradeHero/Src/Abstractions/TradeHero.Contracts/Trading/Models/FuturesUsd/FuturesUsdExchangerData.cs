using Binance.Net.Objects.Models.Futures;

namespace TradeHero.Contracts.Trading.Models.FuturesUsd;

public class FuturesUsdExchangerData
{
    public string StreamListenKey { get; set; } = string.Empty;

    public BinanceFuturesUsdtExchangeInfo ExchangeInfo { get; set; } = new();
}