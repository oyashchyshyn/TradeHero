using TradeHero.Contracts.Base.Enums;

namespace TradeHero.Contracts.StrategyRunner;

public interface ITradeLogicFactory
{
    ITradeLogic? GetTradeLogicRunner(TradeLogicType tradeLogicType);
}