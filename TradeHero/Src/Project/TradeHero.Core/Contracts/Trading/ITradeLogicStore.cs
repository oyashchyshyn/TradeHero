using TradeHero.Core.Models.Trading;

namespace TradeHero.Core.Contracts.Trading;

public interface ITradeLogicStore
{
    SpotMarket Spot { get; }
    FuturesUsdMarket FuturesUsd { get; }
    List<Position> Positions { get; }
}