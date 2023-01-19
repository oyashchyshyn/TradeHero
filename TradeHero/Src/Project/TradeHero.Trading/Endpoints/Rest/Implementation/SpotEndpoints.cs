using Microsoft.Extensions.Logging;
using TradeHero.Core.Enums;
using TradeHero.Core.Exceptions;
using TradeHero.Core.Types.Client;
using TradeHero.Core.Types.Trading;

namespace TradeHero.Trading.Endpoints.Rest.Implementation;

internal class SpotEndpoints : ISpotEndpoints
{
    private readonly ILogger<SpotEndpoints> _logger;
    private readonly IThRestBinanceClient _restBinanceClient;

    public SpotEndpoints(
        ILogger<SpotEndpoints> logger, 
        IThRestBinanceClient restBinanceClient
        )
    {
        _logger = logger;
        _restBinanceClient = restBinanceClient;
    }

    public async Task<ActionResult> SetSpotExchangeInfoAsync(ITradeLogicStore store, int maxRetries = 5, CancellationToken cancellationToken = default)
    {
        try
        {
            for (var i = 0; i < maxRetries; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("CancellationToken is requested. In {Method}",
                        nameof(SetSpotExchangeInfoAsync));

                    return ActionResult.CancellationTokenRequested;
                }
                
                var stopSymbolInfoRequest = await _restBinanceClient.SpotApi.ExchangeData.GetExchangeInfoAsync(
                    ct: cancellationToken
                );
            
                if (stopSymbolInfoRequest.Success)
                {
                    store.Spot.ExchangerData.ExchangeInfo = stopSymbolInfoRequest.Data;
                    
                    break;
                }

                _logger.LogWarning(new ThException(stopSymbolInfoRequest.Error),"In {Method}",
                    nameof(SetSpotExchangeInfoAsync));

                if (i == maxRetries - 1)
                {
                    continue;
                }
                
                _logger.LogError("{Number} retries exceeded. In {Method}",
                    maxRetries, nameof(SetSpotExchangeInfoAsync));

                return ActionResult.ClientError;
            }

            _logger.LogInformation("Set binance spot symbol info data. {SpotSymbolsCount} In {Method}", 
                store.Spot.ExchangerData.ExchangeInfo.Symbols.Count(), nameof(SetSpotExchangeInfoAsync));

            return ActionResult.Success;
        }
        catch (TaskCanceledException taskCanceledException)
        {
            _logger.LogWarning("{Message}. In {Method}",
                taskCanceledException.Message, nameof(SetSpotExchangeInfoAsync));
            
            return ActionResult.CancellationTokenRequested;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(SetSpotExchangeInfoAsync));

            return ActionResult.SystemError;
        }
    }
}