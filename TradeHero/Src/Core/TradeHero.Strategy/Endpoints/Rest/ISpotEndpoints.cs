using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Strategy;

namespace TradeHero.Strategies.Endpoints.Rest;

public interface ISpotEndpoints
{
    Task<ActionResult> SetSpotExchangeInfoAsync(IStrategyStore store, int maxRetries = 5, CancellationToken cancellationToken = default);
}