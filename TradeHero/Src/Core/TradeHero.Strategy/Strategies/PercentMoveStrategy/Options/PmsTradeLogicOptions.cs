using TradeHero.Strategies.Base;

namespace TradeHero.Strategies.Strategies.PercentMoveStrategy.Options;

internal class PmsTradeLogicOptions : BaseTradeOptions
{
    public decimal PricePercentMove { get; set; }
}