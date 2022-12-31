using Binance.Net.Objects.Models.Spot;

namespace TradeHero.Contracts.StrategyRunner.Models.Spot;

public class SpotExchangerData
{
    public BinanceExchangeInfo ExchangeInfo { get; set; } = new();
}