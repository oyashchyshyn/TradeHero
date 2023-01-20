using TradeHero.Core.Enums;
using TradeHero.Core.Types.Trading;

namespace TradeHero.Trading.Endpoints.Rest;

public interface ISpotEndpoints
{
    Task<ActionResult> SetSpotExchangeInfoAsync(ITradeLogicStore store, int maxRetries = 5, CancellationToken cancellationToken = default);
}