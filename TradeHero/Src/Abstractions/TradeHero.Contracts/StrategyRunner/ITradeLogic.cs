using TradeHero.Contracts.Repositories.Models;
using TradeHero.Contracts.StrategyRunner.Models.Args;
using TradeHero.Core.Enums;

namespace TradeHero.Contracts.StrategyRunner;

public interface ITradeLogic
{
    event EventHandler<FuturesUsdOrderReceiveArgs> OnOrderReceive;
    ITradeLogicStore Store { get; }
    Task<ActionResult> UpdateTradeSettingsAsync(StrategyDto strategyDto);
    Task<ActionResult> InitAsync(StrategyDto strategyDto);
    Task<ActionResult> FinishAsync(bool isNeedToUseCancellationToken);
}