using TradeHero.Core.Args;
using TradeHero.Core.Enums;
using TradeHero.Core.Models.Repositories;

namespace TradeHero.Core.Contracts.Trading;

public interface ITradeLogic
{
    event EventHandler<FuturesUsdOrderReceiveArgs> OnOrderReceive;
    ITradeLogicStore Store { get; }
    Task<ActionResult> UpdateTradeSettingsAsync(StrategyDto strategyDto);
    Task<ActionResult> InitAsync(StrategyDto strategyDto);
    Task<ActionResult> FinishAsync(bool isNeedToUseCancellationToken);
}