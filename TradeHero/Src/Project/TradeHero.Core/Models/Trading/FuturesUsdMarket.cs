namespace TradeHero.Core.Models.Trading;

public class FuturesUsdMarket
{
    public FuturesUsdAccountData AccountData { get; } = new();
    public FuturesUsdExchangerData ExchangerData { get; } = new();
}