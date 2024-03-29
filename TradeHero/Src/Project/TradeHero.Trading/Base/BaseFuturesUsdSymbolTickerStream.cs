using Binance.Net.Interfaces;
using CryptoExchange.Net.Sockets;
using Microsoft.Extensions.Logging;
using TradeHero.Core.Contracts.Client;
using TradeHero.Core.Contracts.Trading;
using TradeHero.Core.Enums;
using TradeHero.Core.Exceptions;

namespace TradeHero.Trading.Base;

internal abstract class BaseFuturesUsdSymbolTickerStream : ITickerStream
{
    private readonly IThSocketBinanceClient _socketBinanceClient;

    protected readonly ILogger Logger;
    
    public UpdateSubscription SocketSubscription { get; private set; } = null!;
    public bool IsConnected { get; private set; }
    
    protected BaseFuturesUsdSymbolTickerStream(
        ILogger logger,
        IThSocketBinanceClient socketBinanceClient
        )
    {
        Logger = logger;
        _socketBinanceClient = socketBinanceClient;
    }
    
    protected abstract Task ManageTickerAsync(IBinance24HPrice ticker, CancellationToken cancellationToken = default);
    
    public async Task<ActionResult> StartStreamSymbolTickerAsync(string symbol, int maxRetries = 5, CancellationToken cancellationToken = default)
    {
        try
        {
            for (var i = 0; i < maxRetries; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Logger.LogInformation("CancellationToken is requested. In {Method}",
                        nameof(StartStreamSymbolTickerAsync));

                    return ActionResult.CancellationTokenRequested;
                }

                async void OnMessage(DataEvent<IBinance24HPrice> onMessage)
                {
                    await ManageTickerAsync(onMessage.Data, cancellationToken);
                }

                var socketSubscriptionResult = await _socketBinanceClient.UsdFuturesStreams.SubscribeToTickerUpdatesAsync(symbol, OnMessage, cancellationToken);

                if (socketSubscriptionResult.Success)
                {
                    IsConnected = true;
                    
                    SocketSubscription = socketSubscriptionResult.Data;
                
                    socketSubscriptionResult.Data.Exception += exception =>
                    {
                        Logger.LogError(exception, "Exception from socket event. In {Method}", nameof(StartStreamSymbolTickerAsync));
                    };

                    socketSubscriptionResult.Data.ActivityPaused += () => { Logger.LogWarning("{Symbol}. Server activity paused", symbol); };
                    socketSubscriptionResult.Data.ActivityUnpaused += () => { Logger.LogInformation("{Symbol}. Server activity unpaused", symbol); };
                    socketSubscriptionResult.Data.ConnectionLost += () =>
                    {
                        IsConnected = false;
                        Logger.LogWarning("{Symbol}. Server connection lost", symbol);
                    };
                    socketSubscriptionResult.Data.ConnectionRestored += _ =>
                    {
                        IsConnected = true;
                        Logger.LogInformation("{Symbol}. Server connection established", symbol);
                    };
                    socketSubscriptionResult.Data.ConnectionClosed += () =>
                    {
                        IsConnected = false;
                        Logger.LogInformation("{Symbol}. Server connection closed", symbol);
                    };
                    
                    break;
                }
                
                Logger.LogWarning(new ThException(socketSubscriptionResult.Error),"In {Method}",
                    nameof(StartStreamSymbolTickerAsync));

                if (i != maxRetries - 1)
                {
                    continue;
                }
                
                Logger.LogError("{Symbol}. {Number} retries exceeded In {Method}",
                    symbol, maxRetries, nameof(StartStreamSymbolTickerAsync));

                return ActionResult.ClientError;
            }
            
            Logger.LogInformation("{Symbol}. Successfully subscribed to socket. In {Method}",
                symbol, nameof(StartStreamSymbolTickerAsync));
            
            return ActionResult.Success;
        }
        catch (TaskCanceledException taskCanceledException)
        {
            Logger.LogInformation("{Message}. In {Method}",
                taskCanceledException.Message, nameof(StartStreamSymbolTickerAsync));
            
            return ActionResult.CancellationTokenRequested;
        }
        catch (Exception exception)
        {
            Logger.LogCritical(exception, "{Symbol}. In {Method}", symbol, nameof(StartStreamSymbolTickerAsync));

            return ActionResult.SystemError;
        }
    }
}