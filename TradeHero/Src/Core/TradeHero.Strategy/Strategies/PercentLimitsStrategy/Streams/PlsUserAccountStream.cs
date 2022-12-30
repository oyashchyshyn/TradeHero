using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures.Socket;
using CryptoExchange.Net.Sockets;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Client;
using TradeHero.Contracts.Services;
using TradeHero.Strategies.Base;
using TradeHero.Strategies.Strategies.PercentLimitsStrategy.Flow;

namespace TradeHero.Strategies.Strategies.PercentLimitsStrategy.Streams;

internal class PlsUserAccountStream : BaseFuturesUsdUserAccountStream
{
    private readonly PlsPositionWorker _plsPositionWorker;

    public PlsUserAccountStream(
        ILogger<PlsUserAccountStream> logger,
        IThSocketBinanceClient socketClient,
        IJsonService jsonService,
        PlsStore plsStore, 
        PlsPositionWorker plsPositionWorker
        )
        : base(socketClient, logger, plsStore, jsonService)
    {
        _plsPositionWorker = plsPositionWorker;
    }

    protected override async Task OnOrderUpdateAsync(DataEvent<BinanceFuturesStreamOrderUpdate> data, CancellationToken cancellationToken)
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

                _plsPositionWorker.UpdatePositionQuantity(openedPosition, data.Data, true);

                if (data.Data.UpdateData.Status != OrderStatus.Filled || openedPosition.TotalQuantity > 0)
                {
                    return;
                }

                await _plsPositionWorker.DeletePositionAsync(Store, openedPosition, cancellationToken);
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
                        _plsPositionWorker.UpdatePositionQuantity(openedPosition, data.Data, false);
                    }
                    else
                    {
                        await _plsPositionWorker.CreatePositionAsync(
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