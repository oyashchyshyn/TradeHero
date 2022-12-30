using TradeHero.Contracts.Client.Models.Response;

namespace TradeHero.Contracts.Client.CustomApi;

public interface ISystemApi
{
    Task<ThWebCallResult<bool>> PingMarketsAsync(CancellationToken cancellationToken = default);
}