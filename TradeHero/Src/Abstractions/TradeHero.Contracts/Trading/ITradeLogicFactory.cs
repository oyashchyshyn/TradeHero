using TradeHero.Core.Enums;

namespace TradeHero.Contracts.Trading;

public interface ITradeLogicFactory
{
    ITradeLogic? GetTradeLogicRunner(TradeLogicType tradeLogicType);
}