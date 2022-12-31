using TradeHero.Strategies.Base;

namespace TradeHero.Strategies.TradeLogic.PercentMove.Options;

internal class PercentMoveTradeLogicOptions : BaseTradeLogicOptions
{
    public decimal PricePercentMove { get; set; }
}