using TradeHero.Core.Enums;

namespace TradeHero.Contracts.StrategyRunner;

public interface ITradeLogicFactory
{
    ITradeLogic? GetTradeLogicRunner(TradeLogicType tradeLogicType);
}