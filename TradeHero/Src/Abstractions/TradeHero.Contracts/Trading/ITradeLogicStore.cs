using TradeHero.Contracts.Trading.Models;
using TradeHero.Contracts.Trading.Models.FuturesUsd;
using TradeHero.Contracts.Trading.Models.Spot;

namespace TradeHero.Contracts.Trading;

public interface ITradeLogicStore
{
    SpotMarket Spot { get; }
    FuturesUsdMarket FuturesUsd { get; }
    List<Position> Positions { get; }
}