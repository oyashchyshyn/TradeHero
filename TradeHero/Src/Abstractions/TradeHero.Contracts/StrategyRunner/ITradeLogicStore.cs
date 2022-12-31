using TradeHero.Contracts.StrategyRunner.Models;
using TradeHero.Contracts.StrategyRunner.Models.FuturesUsd;
using TradeHero.Contracts.StrategyRunner.Models.Spot;

namespace TradeHero.Contracts.StrategyRunner;

public interface ITradeLogicStore
{
    SpotMarket Spot { get; }
    FuturesUsdMarket FuturesUsd { get; }
    List<Position> Positions { get; }
}