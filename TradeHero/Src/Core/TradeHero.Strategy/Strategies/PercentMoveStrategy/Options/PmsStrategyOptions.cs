using TradeHero.Strategies.Base;

namespace TradeHero.Strategies.Strategies.PercentMoveStrategy.Options;

internal class PmsStrategyOptions : BaseTradeOptions
{
    public decimal PricePercentMove { get; set; }
}