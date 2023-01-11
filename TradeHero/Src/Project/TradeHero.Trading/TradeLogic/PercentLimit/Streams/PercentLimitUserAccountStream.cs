using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures.Socket;
using CryptoExchange.Net.Sockets;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Client;
using TradeHero.Contracts.Services;
using TradeHero.Contracts.Trading.Models.Args;
using TradeHero.Core.Enums;
using TradeHero.Trading.Base;
using TradeHero.Trading.TradeLogic.PercentLimit.Flow;

namespace TradeHero.Trading.TradeLogic.PercentLimit.Streams;

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

                orderReceiveEvent?.Invoke(this, new FuturesUsdOrderReceiveArgs(data.Data.UpdateData, OrderReceiveType.Close));
                
                _percentLimitPositionWorker.UpdatePositionQuantity(openedPosition, data.Data, true);

                if (data.Data.UpdateData.Status != OrderStatus.Filled || openedPosition.TotalQuantity > 0)
                {
                    return;
                }

                await _percentLimitPositionWorker.DeletePositionAsync(Store, openedPosition, cancellationToken);
            }
            else
            {
                if (data.Data.UpdateData.Type == FuturesOrderType.Market && data.Data.UpdateData.Status is OrderStatus.Filled)
                {
                    var openedPosition = Store.Positions.SingleOrDefault(
                        x => x.Name == data.Data.UpdateData.Symbol && x.PositionSide == data.Data.UpdateData.PositionSide
                    );
                
                    if (openedPosition != null)
                    {
                        orderReceiveEvent?.Invoke(this, new FuturesUsdOrderReceiveArgs(data.Data.UpdateData, OrderReceiveType.Average));
                        
                        _percentLimitPositionWorker.UpdatePositionQuantity(openedPosition, data.Data, false);
                    }
                    else
                    {
                        orderReceiveEvent?.Invoke(this, new FuturesUsdOrderReceiveArgs(data.Data.UpdateData, OrderReceiveType.Open));
                        
                        await _percentLimitPositionWorker.CreatePositionAsync(
                            Store,
                            data.Data.UpdateData.Symbol,
                            data.Data.UpdateData.PositionSide,
                            data.Data.UpdateData.AveragePrice,
                            data.Data.UpdateData.UpdateTime,
                            data.Data.UpdateData.Quantity,
                            false,
                            cancellationToken
                        );
                    }
                }
            }
        }
        catch (TaskCanceledException taskCanceledException)
        {
            Logger.LogWarning("{Message}. In {Method}",
                taskCanceledException.Message, nameof(OnOrderUpdateAsync));
        }
        catch (Exception exception)
        {
            Logger.LogCritical(exception, "In {Method}", nameof(OnOrderUpdateAsync));
        }
    }
}