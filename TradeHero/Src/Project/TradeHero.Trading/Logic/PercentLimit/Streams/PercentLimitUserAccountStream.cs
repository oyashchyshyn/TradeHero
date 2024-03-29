using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures.Socket;
using CryptoExchange.Net.Sockets;
using Microsoft.Extensions.Logging;
using TradeHero.Core.Args;
using TradeHero.Core.Contracts.Client;
using TradeHero.Core.Contracts.Services;
using TradeHero.Core.Enums;
using TradeHero.Trading.Base;
using TradeHero.Trading.Logic.PercentLimit.Flow;

namespace TradeHero.Trading.Logic.PercentLimit.Streams;

internal class PercentLimitUserAccountStream : BaseFuturesUsdUserAccountStream
{
    private readonly PercentLimitPositionWorker _percentLimitPositionWorker;

    public PercentLimitUserAccountStream(
        ILogger<PercentLimitUserAccountStream> logger,
        IThSocketBinanceClient socketClient,
        IJsonService jsonService,
        PercentLimitStore percentLimitStore, 
        PercentLimitPositionWorker percentLimitPositionWorker
        )
        : base(socketClient, logger, percentLimitStore, jsonService)
    {
        _percentLimitPositionWorker = percentLimitPositionWorker;
    }

    protected override async Task OnOrderUpdateAsync(DataEvent<BinanceFuturesStreamOrderUpdate> data, 
        EventHandler<FuturesUsdOrderReceiveArgs>? orderReceiveEvent, CancellationToken cancellationToken)
    {
        try
        {
            Logger.LogInformation("{Method}. Data: {Data}", nameof(OnOrderUpdateAsync), JsonService.SerializeObject(data.Data).Data);
            
            // When order has realized profit
            if (data.Data.UpdateData.RealizedProfit != 0)
            {
                var openedPosition = Store.Positions.SingleOrDefault(
                    x => x.Name == data.Data.UpdateData.Symbol && x.PositionSide == data.Data.UpdateData.PositionSide
                );
            
                if (openedPosition == null)
                {
                    return;
                }
                
                _percentLimitPositionWorker.UpdatePositionQuantity(openedPosition, data.Data, true);

                if (data.Data.UpdateData.Status != OrderStatus.Filled || openedPosition.TotalQuantity is > 0 or < 0)
                {
                    orderReceiveEvent?.Invoke(this, new FuturesUsdOrderReceiveArgs(data.Data.UpdateData, OrderReceiveType.PartialClosed));
                    
                    return;
                }

                await _percentLimitPositionWorker.DeletePositionAsync(Store, openedPosition, cancellationToken);
                
                orderReceiveEvent?.Invoke(this, new FuturesUsdOrderReceiveArgs(data.Data.UpdateData, OrderReceiveType.FullyClosed));
            }
            else
            {
                if (data.Data.UpdateData is { Type: FuturesOrderType.Market, Status: OrderStatus.Filled or OrderStatus.PartiallyFilled })
                {
                    var openedPosition = Store.Positions.SingleOrDefault(
                        x => x.Name == data.Data.UpdateData.Symbol && x.PositionSide == data.Data.UpdateData.PositionSide
                    );
                
                    if (openedPosition != null)
                    {
                        _percentLimitPositionWorker.UpdatePositionQuantity(openedPosition, data.Data, false);
                        
                        orderReceiveEvent?.Invoke(this, new FuturesUsdOrderReceiveArgs(data.Data.UpdateData, OrderReceiveType.Average));
                    }
                    else
                    {
                        await _percentLimitPositionWorker.CreatePositionAsync(
                            Store,
                            data.Data.UpdateData.Symbol,
                            data.Data.UpdateData.PositionSide,
                            data.Data.UpdateData.AveragePrice,
                            data.Data.UpdateData.UpdateTime,
                            data.Data.UpdateData.QuantityOfLastFilledTrade,
                            false,
                            cancellationToken
                        );
                        
                        orderReceiveEvent?.Invoke(this, new FuturesUsdOrderReceiveArgs(data.Data.UpdateData, OrderReceiveType.Open));
                    }
                }
            }
        }
        catch (TaskCanceledException taskCanceledException)
        {
            Logger.LogInformation("{Message}. In {Method}",
                taskCanceledException.Message, nameof(OnOrderUpdateAsync));
        }
        catch (Exception exception)
        {
            Logger.LogCritical(exception, "In {Method}", nameof(OnOrderUpdateAsync));
        }
    }
}