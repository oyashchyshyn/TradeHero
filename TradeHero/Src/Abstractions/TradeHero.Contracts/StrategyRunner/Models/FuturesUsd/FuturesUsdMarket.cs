namespace TradeHero.Contracts.StrategyRunner.Models.FuturesUsd;

public class FuturesUsdMarket
{
    public FuturesUsdAccountData AccountData { get; } = new();
    public FuturesUsdExchangerData ExchangerData { get; } = new();
}