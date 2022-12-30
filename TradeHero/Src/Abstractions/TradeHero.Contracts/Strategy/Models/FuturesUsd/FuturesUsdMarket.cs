namespace TradeHero.Contracts.Strategy.Models.FuturesUsd;

public class FuturesUsdMarket
{
    public FuturesUsdAccountData AccountData { get; } = new();
    public FuturesUsdExchangerData ExchangerData { get; } = new();
}