using TradeHero.Core.Enums;
using TradeHero.Core.Types.Repositories.Models;
using TradeHero.Core.Types.Trading.Models.Args;

namespace TradeHero.Core.Types.Trading;

public interface ITradeLogic
{
    event EventHandler<FuturesUsdOrderReceiveArgs> OnOrderReceive;
    ITradeLogicStore Store { get; }
    Task<ActionResult> UpdateTradeSettingsAsync(StrategyDto strategyDto);
    Task<ActionResult> InitAsync(StrategyDto strategyDto);
    Task<ActionResult> FinishAsync(bool isNeedToUseCancellationToken);
}