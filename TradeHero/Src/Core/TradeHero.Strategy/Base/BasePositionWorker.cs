using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures;
using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Strategy;
using TradeHero.Contracts.Strategy.Models;

namespace TradeHero.Strategies.Base;

public abstract class BasePositionWorker
{
    public abstract Task<ActionResult> CreatePositionAsync(IStrategyStore strategyStore, string symbol, PositionSide side, decimal entryPrice, DateTime lastUpdateTime, 
        decimal quantity, bool isOrderExist, CancellationToken cancellationToken);
    public abstract ActionResult UpdatePositionDetails(IStrategyStore strategyStore, Position openedPosition, BinancePositionDetailsUsdt positionDetails);
    public abstract Task<ActionResult> DeletePositionAsync(IStrategyStore strategyStore, Position openedPosition, CancellationToken cancellationToken);
}