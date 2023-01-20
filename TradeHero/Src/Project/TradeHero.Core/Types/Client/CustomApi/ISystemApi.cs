using TradeHero.Core.Types.Client.Models.Response;

namespace TradeHero.Core.Types.Client.CustomApi;

public interface ISystemApi
{
    Task<ThWebCallResult<bool>> PingMarketsAsync(CancellationToken cancellationToken = default);
}