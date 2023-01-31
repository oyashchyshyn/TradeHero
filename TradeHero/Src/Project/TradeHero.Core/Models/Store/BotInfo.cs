using TradeHero.Core.Contracts.Trading;
using TradeHero.Core.Enums;

namespace TradeHero.Core.Models.Store;

public class BotInfo
{
    public event EventHandler? OnTradeLogicUpdate;

    public TradeLogicStatus TradeLogicStatus { get; private set; } = TradeLogicStatus.Idle;
    public ITradeLogic? TradeLogic { get; private set; }

    public void SetTradeLogic(ITradeLogic? tradeLogic, TradeLogicStatus tradeLogicStatus)
    {
        TradeLogic = tradeLogic;
        TradeLogicStatus = tradeLogicStatus;

        OnTradeLogicUpdate?.Invoke(this, EventArgs.Empty);
    }
}