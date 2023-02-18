namespace TradeHero.Trading.Logic.PercentLimit.Options;

internal class SignalSettings
{
    public SignalOptions LongMarketMood { get; set; } = new();
    public SignalOptions ShortMarketMood { get; set; } = new();
    public SignalOptions BalancedMarketMood { get; set; } = new();
}