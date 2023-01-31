using TradeHero.Core.Contracts.Trading;
using TradeHero.Core.Enums;

namespace TradeHero.Trading.Endpoints.Rest;

public interface ISpotEndpoints
{
    Task<ActionResult> SetSpotExchangeInfoAsync(ITradeLogicStore store, int maxRetries = 5, CancellationToken cancellationToken = default);
}