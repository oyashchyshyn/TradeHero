namespace TradeHero.Trading.Logic.PercentLimit.Options;

internal class SignalOptions
{
    public PositionOption Long { get; set; } = new();
    public PositionOption Short { get; set; } = new();
}