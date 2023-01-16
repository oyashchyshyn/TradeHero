using TradeHero.Trading.Base;

namespace TradeHero.Trading.Logic.PercentMove.Options;

internal class PercentMoveTradeLogicOptions : BaseTradeLogicOptions
{
    public decimal PricePercentMove { get; set; }
}