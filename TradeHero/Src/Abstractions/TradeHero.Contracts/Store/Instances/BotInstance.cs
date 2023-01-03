using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.StrategyRunner;

namespace TradeHero.Contracts.Store.Instances;

public class BotInstance
{
    public TradeLogicStatus TradeLogicStatus { get; private set; } = TradeLogicStatus.Idle;
    public ITradeLogic? TradeLogic { get; private set; }

    public void SetTradeLogic(ITradeLogic? tradeLogic, TradeLogicStatus tradeLogicStatus)
    {
        TradeLogic = tradeLogic;
        TradeLogicStatus = tradeLogicStatus;
    }
}