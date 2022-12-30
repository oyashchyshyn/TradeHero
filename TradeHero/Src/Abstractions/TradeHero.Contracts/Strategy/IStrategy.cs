using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Repositories.Models;

namespace TradeHero.Contracts.Strategy;

public interface IStrategy
{
    IStrategyStore Store { get; }
    Task<ActionResult> UpdateTradeSettingsAsync(StrategyDto strategyDto);
    Task<ActionResult> InitAsync(StrategyDto strategyDto);
    Task<ActionResult> FinishAsync(bool isNeedToUseCancellationToken);
}