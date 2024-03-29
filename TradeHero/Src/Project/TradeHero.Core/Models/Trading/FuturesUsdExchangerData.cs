using Binance.Net.Objects.Models.Futures;

namespace TradeHero.Core.Models.Trading;

public class FuturesUsdExchangerData
{
    public string StreamListenKey { get; set; } = string.Empty;

    public BinanceFuturesUsdtExchangeInfo ExchangeInfo { get; set; } = new();
}