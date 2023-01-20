using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures;
using TradeHero.Core.Enums;
using TradeHero.Core.Types.Trading;
using TradeHero.Core.Types.Trading.Models;

namespace TradeHero.Trading.Base;

public abstract class BasePositionWorker
{
    public abstract Task<ActionResult> CreatePositionAsync(ITradeLogicStore tradeLogicStore, string symbol, PositionSide side, decimal entryPrice, DateTime lastUpdateTime, 
        decimal quantity, bool isOrderExist, CancellationToken cancellationToken);
    public abstract ActionResult UpdatePositionDetails(ITradeLogicStore tradeLogicStore, Position openedPosition, BinancePositionDetailsUsdt positionDetails);
    public abstract Task<ActionResult> DeletePositionAsync(ITradeLogicStore tradeLogicStore, Position openedPosition, CancellationToken cancellationToken);
}