using TradeHero.Core.Models.Client;

namespace TradeHero.Core.Contracts.Client;

public interface ISystemApi
{
    Task<ThWebCallResult<bool>> PingMarketsAsync(CancellationToken cancellationToken = default);
}