using TradeHero.Contracts.Trading;
using TradeHero.Core.Enums;

namespace TradeHero.Contracts.Services.Models.Store;

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