using TradeHero.Core.Enums;

namespace TradeHero.Core.Contracts.Trading;

public interface ITradeLogicFactory
{
    ITradeLogic? GetTradeLogicRunner(TradeLogicType tradeLogicType);
}