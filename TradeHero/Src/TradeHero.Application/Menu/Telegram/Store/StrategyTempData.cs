using TradeHero.Core.Enums;

namespace TradeHero.Application.Menu.Telegram.Store;

internal class StrategyTempData
{
    public string StrategyId { get; set; } = string.Empty;
    public string StrategyName { get; set; } = string.Empty;
    public string StrategyJson { get; set; } = string.Empty;
    public string InstanceJson { get; set; } = string.Empty;
    public TradeLogicType TradeLogicType { get; set; }
    public InstanceType InstanceType { get; set; }
    public StrategyObject StrategyObjectToUpdate { get; set; }

    public void ClearData()
    {
        StrategyId = string.Empty;
        StrategyJson = string.Empty;
        InstanceJson = string.Empty;
        TradeLogicType = TradeLogicType.NoTradeLogic;
        InstanceType = InstanceType.NoInstance;
        StrategyObjectToUpdate = StrategyObject.None;
    }
}