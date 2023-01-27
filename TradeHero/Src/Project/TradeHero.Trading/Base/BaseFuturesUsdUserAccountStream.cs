using Binance.Net.Objects.Models;
using Binance.Net.Objects.Models.Futures.Socket;
using CryptoExchange.Net.Sockets;
using Microsoft.Extensions.Logging;
using TradeHero.Core.Enums;
using TradeHero.Core.Exceptions;
using TradeHero.Core.Types.Client;
using TradeHero.Core.Types.Services;
using TradeHero.Core.Types.Trading;
using TradeHero.Core.Types.Trading.Models.Args;

namespace TradeHero.Trading.Base;

internal abstract class BaseFuturesUsdUserAccountStream
{
    private readonly IThSocketBinanceClient _binanceSocketClient;
    
    protected readonly ILogger Logger;
    protected readonly ITradeLogicStore Store;
    protected readonly IJsonService JsonService;
    
    public UpdateSubscription SocketSubscription { get; private set; } = null!;
    
    protected BaseFuturesUsdUserAccountStream(
        IThSocketBinanceClient binanceSocketClient,
        ILogger logger,
        ITradeLogicStore store,
        IJsonService jsonService
        )
    {
        _binanceSocketClient = binanceSocketClient;
        
        Logger = logger;
        Store = store;
        JsonService = jsonService;
    }
    
    protected abstract Task OnOrderUpdateAsync(DataEvent<BinanceFuturesStreamOrderUpdate> data, 
        EventHandler<FuturesUsdOrderReceiveArgs>? orderReceiveEvent, CancellationToken cancellationToken);
    
    public async Task<ActionResult> StartUserUpdateDataStreamAsync(EventHandler<FuturesUsdOrderReceiveArgs>? orderReceiveEvent, 
        int maxRetries = 5, CancellationToken cancellationToken = default)
    {
        try
        {
            for (var i = 0; i < maxRetries; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Logger.LogInformation("CancellationToken is requested. In {Method}",
                        nameof(StartUserUpdateDataStreamAsync));

                    return ActionResult.CancellationTokenRequested;
                }

                async void OnOrderUpdate(DataEvent<BinanceFuturesStreamOrderUpdate> data) => await OnOrderUpdateAsync(data, orderReceiveEvent, cancellationToken);

                var socketSubscriptionResult = await _binanceSocketClient.UsdFuturesStreams.SubscribeToUserDataUpdatesAsync(
                    Store.FuturesUsd.ExchangerData.StreamListenKey,
                    OnLeverageUpdate,
                    OnMarginUpdate,
                    OnAccountUpdate,
                    OnOrderUpdate,
                    OnListenKeyExpired,
                    cancellationToken
                );

                if (socketSubscriptionResult.Success)
                {
                    SocketSubscription = socketSubscriptionResult.Data;
                
                    socketSubscriptionResult.Data.Exception += exception =>
                    {
                        Logger.LogError(exception, "In {Method}", nameof(socketSubscriptionResult.Data.Exception));
                    };

                    socketSubscriptionResult.Data.ActivityPaused += () => { Logger.LogWarning("Server activity paused"); };
                    socketSubscriptionResult.Data.ActivityUnpaused += () => { Logger.LogInformation("Server activity unpaused"); };
                    socketSubscriptionResult.Data.ConnectionLost += () => { Logger.LogWarning("Server connection lost"); };
                    socketSubscriptionResult.Data.ConnectionRestored += _ => { Logger.LogInformation("Server connection established"); };
                    socketSubscriptionResult.Data.ConnectionClosed += () => { Logger.LogInformation("Server connection closed"); };
                    
                    break;
                }

                Logger.LogWarning(new ThException(socketSubscriptionResult.Error),"In {Method}",
                    nameof(StartUserUpdateDataStreamAsync));

                if (i != maxRetries - 1)
                {
                    continue;
                }
                
                Logger.LogError("{Number} retries exceeded In {Method}",
                    maxRetries, nameof(StartUserUpdateDataStreamAsync));

                return ActionResult.ClientError;
            }
            
            Logger.LogInformation("Successfully subscribed to socket. In {Method}",
                nameof(StartUserUpdateDataStreamAsync));
            
            return ActionResult.Success;
        }
        catch (TaskCanceledException taskCanceledException)
        {
            Logger.LogInformation("{Message}. In {Method}",
                taskCanceledException.Message, nameof(StartUserUpdateDataStreamAsync));
            
            return ActionResult.CancellationTokenRequested;
        }
        catch (Exception exception)
        {
            Logger.LogCritical(exception, "In {Method}", nameof(StartUserUpdateDataStreamAsync));

            return ActionResult.SystemError;
        }
    }

    #region Private methods

    private void OnAccountUpdate(DataEvent<BinanceFuturesStreamAccountUpdate> data)
    {
        try
        {
            Logger.LogInformation("OnAccountUpdate. Data: {Data}", JsonService.SerializeObject(data.Data).Data);
            
            foreach (var position in data.Data.UpdateData.Positions.Where(x => x.EntryPrice > 0))
            {
                try
                {
                    var openedPosition = Store.Positions.SingleOrDefault(
                        x => x.Name == position.Symbol 
                             && x.PositionSide == position.PositionSide
                    );
                
                    if (openedPosition == null)
                    {
                        return;
                    }

                    openedPosition.EntryPrice = position.EntryPrice;
                }
                catch (Exception exception)
                {
                    Logger.LogError(exception, "Symbol: {Symbol}, Position: {Position}. In {Method}",
                        position.Symbol, position.PositionSide, nameof(OnAccountUpdate));
                }
            }
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "In {Method}", nameof(OnAccountUpdate));
        }
    }
    
    private void OnLeverageUpdate(DataEvent<BinanceFuturesStreamConfigUpdate> data)
    {
        try
        {
            Logger.LogInformation("OnLeverageUpdate. Data: {Data}", JsonService.SerializeObject(data.Data).Data);
            
            if (string.IsNullOrWhiteSpace(data.Data.LeverageUpdateData.Symbol))
            {
                Logger.LogWarning("OnLeverageUpdate. Symbol is null. In {Method}", nameof(OnLeverageUpdate));
                
                return;
            }
            
            var positions = Store.FuturesUsd.AccountData.Positions
                .Where(x => x.Symbol == data.Data.LeverageUpdateData.Symbol);

            foreach (var position in positions)
            {
                position.Leverage = data.Data.LeverageUpdateData.Leverage;
            }

            foreach (var position in Store.Positions.Where(x => x.Name == data.Data.LeverageUpdateData.Symbol))
            {
                position.Leverage = data.Data.LeverageUpdateData.Leverage; 
            }
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "In {Method}", nameof(OnLeverageUpdate));
        }
    }
    
    private void OnMarginUpdate(DataEvent<BinanceFuturesStreamMarginUpdate> data)
    {
        Logger.LogInformation("OnMarginUpdate. Data: {Data}", JsonService.SerializeObject(data.Data).Data);
    }
    
    private void OnListenKeyExpired(DataEvent<BinanceStreamEvent> data)
    {
        Logger.LogInformation("OnListenKeyExpiredUpdate. Data: {Data}", JsonService.SerializeObject(data.Data).Data);
    }

    #endregion
}