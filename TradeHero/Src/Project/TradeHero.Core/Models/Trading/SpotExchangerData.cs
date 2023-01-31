using Binance.Net.Objects.Models.Spot;

namespace TradeHero.Core.Models.Trading;

public class SpotExchangerData
{
    public BinanceExchangeInfo ExchangeInfo { get; set; } = new();
}