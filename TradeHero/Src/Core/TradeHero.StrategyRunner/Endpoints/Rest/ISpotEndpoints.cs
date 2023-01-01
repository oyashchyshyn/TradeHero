using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.StrategyRunner;

namespace TradeHero.Strategies.Endpoints.Rest;

public interface ISpotEndpoints
{
    Task<ActionResult> SetSpotExchangeInfoAsync(ITradeLogicStore store, int maxRetries = 5, CancellationToken cancellationToken = default);
}