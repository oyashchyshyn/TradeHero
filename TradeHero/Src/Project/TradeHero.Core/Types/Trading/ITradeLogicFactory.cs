using TradeHero.Core.Enums;

namespace TradeHero.Core.Types.Trading;

public interface ITradeLogicFactory
{
    ITradeLogic? GetTradeLogicRunner(TradeLogicType tradeLogicType);
}