using Binance.Net.Objects.Models.Spot;

namespace TradeHero.Core.Types.Trading.Models.Spot;

public class SpotExchangerData
{
    public BinanceExchangeInfo ExchangeInfo { get; set; } = new();
}