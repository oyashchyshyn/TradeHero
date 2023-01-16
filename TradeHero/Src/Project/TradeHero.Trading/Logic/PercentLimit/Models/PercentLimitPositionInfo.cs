namespace TradeHero.Trading.Logic.PercentLimit.Models;

public class PercentLimitPositionInfo
{
    public decimal HighestRoe { get; set; }
    public bool IsTrailingStopActivated { get; set; }
    public bool IsNeedToCheckPosition { get; set; }
    public bool IsNeedToPlaceMarketStop { get; set; }
}