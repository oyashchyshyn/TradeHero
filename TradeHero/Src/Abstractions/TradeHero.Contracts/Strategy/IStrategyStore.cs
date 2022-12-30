using TradeHero.Contracts.Strategy.Models;
using TradeHero.Contracts.Strategy.Models.FuturesUsd;
using TradeHero.Contracts.Strategy.Models.Spot;

namespace TradeHero.Contracts.Strategy;

public interface IStrategyStore
{
    SpotMarket Spot { get; }
    FuturesUsdMarket FuturesUsd { get; }
    List<Position> Positions { get; }
}