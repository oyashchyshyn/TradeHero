using TradeHero.Core.Types.Trading.Models;
using TradeHero.Core.Types.Trading.Models.FuturesUsd;
using TradeHero.Core.Types.Trading.Models.Spot;

namespace TradeHero.Core.Types.Trading;

public interface ITradeLogicStore
{
    SpotMarket Spot { get; }
    FuturesUsdMarket FuturesUsd { get; }
    List<Position> Positions { get; }
}