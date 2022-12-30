using TradeHero.Contracts.Client;
using TradeHero.Contracts.Client.CustomApi;
using TradeHero.Contracts.Client.Models.Response;

namespace TradeHero.Client.CustomApi;

internal class SystemApi : ISystemApi
{
    private readonly IThRestBinanceClient _client;
    
    public SystemApi(IThRestBinanceClient client)
    {
        _client = client;
    }
    
    public async Task<ThWebCallResult<bool>> PingMarketsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var spotPingRequest = await _client.SpotApi.ExchangeData.PingAsync(cancellationToken);
            if (!spotPingRequest.Success)
            {
                return new ThWebCallResult<bool>(spotPingRequest.Error);
            }

            var futuresUsdPingRequest = await _client.UsdFuturesApi.ExchangeData.PingAsync(cancellationToken);
            if (!futuresUsdPingRequest.Success)
            {
                return new ThWebCallResult<bool>(futuresUsdPingRequest.Error);
            }
            
            var coinFuturesPingRequest = await _client.CoinFuturesApi.ExchangeData.PingAsync(cancellationToken);
            return !coinFuturesPingRequest.Success ? new ThWebCallResult<bool>(coinFuturesPingRequest.Error) : new ThWebCallResult<bool>(true);
        }
        catch (Exception exception)
        {
            return new ThWebCallResult<bool>(new ThError(exception));
        }
    }
}