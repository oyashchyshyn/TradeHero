namespace TradeHero.Strategies.Strategies.PercentLimitsStrategy.Models;

public class PositionInfo
{
    public decimal HighestRoe { get; set; }
    public bool IsTrailingStopActivated { get; set; }
    public bool IsNeedToCheckPosition { get; set; }
    public bool IsNeedToPlaceMarketStop { get; set; }
}