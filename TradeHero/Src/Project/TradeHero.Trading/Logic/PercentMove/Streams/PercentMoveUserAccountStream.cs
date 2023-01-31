using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures.Socket;
using CryptoExchange.Net.Sockets;
using Microsoft.Extensions.Logging;
using TradeHero.Core.Args;
using TradeHero.Core.Contracts.Client;
using TradeHero.Core.Contracts.Services;
using TradeHero.Core.Enums;
using TradeHero.Trading.Base;
using TradeHero.Trading.Logic.PercentMove.Flow;

namespace TradeHero.Trading.Logic.PercentMove.Streams;

internal class PercentMoveUserAccountStream : BaseFuturesUsdUserAccountStream
{
    private readonly PercentMovePositionWorker _percentMovePositionWorker;

    public PercentMoveUserAccountStream(
        ILogger<PercentMoveUserAccountStream> logger,
        IThSocketBinanceClient socketClient,
        IJsonService jsonService,
        PercentMoveStore percentMoveStore, 
        PercentMovePositionWorker percentMovePositionWorker
        )
        : base(socketClient, logger, percentMoveStore, jsonService)
    {
        _percentMovePositionWorker = percentMovePositionWorker;
    }
    
    protected override async Task OnOrderUpdateAsync(DataEvent<BinanceFuturesStreamOrderUpdate> data, 
        EventHandler<FuturesUsdOrderReceiveArgs>? orderReceiveEvent, CancellationToken cancellationToken)
    {
        try
        {
            // When order has realized profit
            if (data.Data.UpdateData.RealizedProfit != 0)
            {
                var openedPosition = Store.Positions.SingleOrDefault(
                    x => x.Name == data.Data.UpdateData.Symbol 
                         && x.PositionSide == data.Data.UpdateData.PositionSide
                );
                
                if (openedPosition == null)
                {
                    return;
                }

                orderReceiveEvent?.Invoke(this, new FuturesUsdOrderReceiveArgs(data.Data.UpdateData, OrderReceiveType.Close));
                
                _percentMovePositionWorker.UpdatePositionQuantity(Store, openedPosition, data.Data, true);

                if (data.Data.UpdateData.Status != OrderStatus.Filled || openedPosition.TotalQuantity is > 0 or < 0)
                {
                    return;
                }

                await _percentMovePositionWorker.DeletePositionAsync(Store, openedPosition, cancellationToken);
            }
            else
            {
                if (data.Data.UpdateData.Type == FuturesOrderType.Market && data.Data.UpdateData.Status is OrderStatus.Filled)
                {
                    var openedPosition = Store.Positions.SingleOrDefault(
                        x => x.Name == data.Data.UpdateData.Symbol 
                             && x.PositionSide == data.Data.UpdateData.PositionSide
                    );
                    
                    if (openedPosition != null)
                    {
                        orderReceiveEvent?.Invoke(this, new FuturesUsdOrderReceiveArgs(data.Data.UpdateData, OrderReceiveType.Average));
                        
                        _percentMovePositionWorker.UpdatePositionQuantity(Store, openedPosition, data.Data, false);
                    }
                    else
                    {
                        orderReceiveEvent?.Invoke(this, new FuturesUsdOrderReceiveArgs(data.Data.UpdateData, OrderReceiveType.Open));
                        
                        await _percentMovePositionWorker.CreatePositionAsync(
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
            Logger.LogInformation("{Message}. In {Method}",
                taskCanceledException.Message, nameof(OnOrderUpdateAsync));
        }
        catch (Exception exception)
        {
            Logger.LogCritical(exception, "In {Method}", nameof(OnOrderUpdateAsync));
        }
    }
}