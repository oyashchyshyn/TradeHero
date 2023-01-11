using TradeHero.Trading.Base;

namespace TradeHero.Trading.TradeLogic.PercentMove.Options;

internal class PercentMoveTradeLogicOptions : BaseTradeLogicOptions
{
    public decimal PricePercentMove { get; set; }
}