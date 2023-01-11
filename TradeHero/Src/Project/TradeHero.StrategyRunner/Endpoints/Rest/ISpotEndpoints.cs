using TradeHero.Contracts.StrategyRunner;
using TradeHero.Core.Enums;

namespace TradeHero.StrategyRunner.Endpoints.Rest;

public interface ISpotEndpoints
{
    Task<ActionResult> SetSpotExchangeInfoAsync(ITradeLogicStore store, int maxRetries = 5, CancellationToken cancellationToken = default);
}