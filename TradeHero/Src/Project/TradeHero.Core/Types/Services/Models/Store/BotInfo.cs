using TradeHero.Core.Enums;
using TradeHero.Core.Types.Trading;

namespace TradeHero.Core.Types.Services.Models.Store;

public class BotInfo
{
    public TradeLogicStatus TradeLogicStatus { get; private set; } = TradeLogicStatus.Idle;
    public ITradeLogic? TradeLogic { get; private set; }

    public void SetTradeLogic(ITradeLogic? tradeLogic, TradeLogicStatus tradeLogicStatus)
    {
        TradeLogic = tradeLogic;
        TradeLogicStatus = tradeLogicStatus;
    }
}