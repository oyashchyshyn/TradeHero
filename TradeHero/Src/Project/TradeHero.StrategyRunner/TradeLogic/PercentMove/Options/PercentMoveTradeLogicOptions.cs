using TradeHero.StrategyRunner.Base;

namespace TradeHero.StrategyRunner.TradeLogic.PercentMove.Options;

internal class PercentMoveTradeLogicOptions : BaseTradeLogicOptions
{
    public decimal PricePercentMove { get; set; }
}