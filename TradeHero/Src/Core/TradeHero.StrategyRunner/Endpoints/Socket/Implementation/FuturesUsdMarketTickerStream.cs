using Binance.Net.Interfaces;
using CryptoExchange.Net.Sockets;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Base.Exceptions;
using TradeHero.Contracts.Client;
using TradeHero.Contracts.StrategyRunner;
using TradeHero.StrategyRunner.Base;

namespace TradeHero.StrategyRunner.Endpoints.Socket.Implementation;

internal class FuturesUsdMarketTickerStream : IFuturesUsdMarketTickerStream
{
    private readonly ILogger<FuturesUsdMarketTickerStream> _logger;
    private readonly IThSocketBinanceClient _socketBinanceClient;

    public UpdateSubscription SocketSubscription { get; private set; } = null!;
    
    public FuturesUsdMarketTickerStream(
        ILogger<FuturesUsdMarketTickerStream> logger,
        IThSocketBinanceClient socketBinanceClient
        )
    {
        _logger = logger;
        _socketBinanceClient = socketBinanceClient;
    }

    public async Task<ActionResult> StartStreamMarketTickerAsync(ITradeLogicStore store, int maxRetries = 5, CancellationToken cancellationToken = default)
    {
        try
        {
            for (var i = 0; i < maxRetries; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("CancellationToken is requested. In {Method}",
                        nameof(StartStreamMarketTickerAsync));

                    return ActionResult.CancellationTokenRequested;
                }

                void OnMessage(DataEvent<IEnumerable<IBinance24HPrice>> onMessage)
                {
                    foreach (var binance24HPrice in onMessage.Data)
                    {
                        if (((BaseTradeLogicStore)store).MarketLastPrices.ContainsKey(binance24HPrice.Symbol))
                        {
                            ((BaseTradeLogicStore)store).MarketLastPrices[binance24HPrice.Symbol] = binance24HPrice.LastPrice;
                        }
                        else
                        {
                            ((BaseTradeLogicStore)store).MarketLastPrices.Add(binance24HPrice.Symbol, binance24HPrice.LastPrice);
                        }
                    }
                }

                var socketSubscriptionResult = await _socketBinanceClient.UsdFuturesStreams.SubscribeToAllTickerUpdatesAsync(OnMessage, cancellationToken);

                if (socketSubscriptionResult.Success)
                {
                    SocketSubscription = socketSubscriptionResult.Data;
                
                    socketSubscriptionResult.Data.Exception += exception =>
                    {
                        _logger.LogError(exception, "Exception from socket event. In {Method}", nameof(StartStreamMarketTickerAsync));
                    };

                    socketSubscriptionResult.Data.ActivityPaused += () => { _logger.LogWarning("Server activity paused"); };
                    socketSubscriptionResult.Data.ActivityUnpaused += () => { _logger.LogInformation("Server activity unpaused"); };
                    socketSubscriptionResult.Data.ConnectionLost += () => { _logger.LogWarning("Server connection lost"); };
                    socketSubscriptionResult.Data.ConnectionRestored += _ => { _logger.LogInformation("Server connection established"); };
                    socketSubscriptionResult.Data.ConnectionClosed += () => { _logger.LogInformation("Server connection closed"); };
                    
                    break;
                }
                
                _logger.LogWarning(new ThException(socketSubscriptionResult.Error),"In {Method}",
                    nameof(StartStreamMarketTickerAsync));

                if (i != maxRetries - 1)
                {
                    continue;
                }
                
                _logger.LogError("{Number} retries exceeded In {Method}",
                    maxRetries, nameof(StartStreamMarketTickerAsync));

                return ActionResult.ClientError;
            }
            
            _logger.LogInformation("Successfully subscribed to socket. In {Method}",
                nameof(StartStreamMarketTickerAsync));
            
            return ActionResult.Success;
        }
        catch (TaskCanceledException taskCanceledException)
        {
            _logger.LogWarning("{Message}. In {Method}",
                taskCanceledException.Message, nameof(StartStreamMarketTickerAsync));
            
            return ActionResult.CancellationTokenRequested;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(StartStreamMarketTickerAsync));

            return ActionResult.SystemError;
        }
    }
}