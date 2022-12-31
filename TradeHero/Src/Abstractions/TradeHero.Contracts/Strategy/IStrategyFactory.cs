using TradeHero.Contracts.Base.Enums;

namespace TradeHero.Contracts.Strategy;

public interface IStrategyFactory
{
    IStrategy? GetStrategy(TradeLogicType tradeLogicType);
}