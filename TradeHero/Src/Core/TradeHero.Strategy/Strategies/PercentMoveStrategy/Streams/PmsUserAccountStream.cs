using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures.Socket;
using CryptoExchange.Net.Sockets;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Client;
using TradeHero.Contracts.Services;
using TradeHero.Strategies.Base;
using TradeHero.Strategies.Strategies.PercentMoveStrategy.Flow;

namespace TradeHero.Strategies.Strategies.PercentMoveStrategy.Streams;

internal class PmsUserAccountStream : BaseFuturesUsdUserAccountStream
{
    private readonly PmsPositionWorker _pmsPositionWorker;

    public PmsUserAccountStream(
        ILogger<PmsUserAccountStream> logger,
        IThSocketBinanceClient socketClient,
        IJsonService jsonService,
        PmsStore pmsStore, 
        PmsPositionWorker pmsPositionWorker
        )
        : base(socketClient, logger, pmsStore, jsonService)
    {
        _pmsPositionWorker = pmsPositionWorker;
    }
    
    protected override async Task OnOrderUpdateAsync(DataEvent<BinanceFuturesStreamOrderUpdate> data, CancellationToken cancellationToken)
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

                _pmsPositionWorker.UpdatePositionQuantity(Store, openedPosition, data.Data, true);

                if (data.Data.UpdateData.Status != OrderStatus.Filled || openedPosition.TotalQuantity > 0)
                {
                    return;
                }

                await _pmsPositionWorker.DeletePositionAsync(Store, openedPosition, cancellationToken);
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
                        _pmsPositionWorker.UpdatePositionQuantity(Store, openedPosition, data.Data, false);
                    }
                    else
                    {
                        await _pmsPositionWorker.CreatePositionAsync(
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