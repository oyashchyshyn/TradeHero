using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures;
using Binance.Net.Objects.Models.Futures.Socket;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Client;
using TradeHero.Contracts.Services;
using TradeHero.Contracts.Trading;
using TradeHero.Contracts.Trading.Models;
using TradeHero.Core.Enums;
using TradeHero.Core.Exceptions;
using TradeHero.Trading.Base;
using TradeHero.Trading.Logic.PercentMove.Factory;

namespace TradeHero.Trading.Logic.PercentMove.Flow;

internal class PercentMovePositionWorker : BasePositionWorker
{
    private readonly ILogger<PercentMovePositionWorker> _logger;
    private readonly IThRestBinanceClient _restBinanceClient;
    private readonly IThSocketBinanceClient _socketBinanceClient;
    private readonly IDateTimeService _dateTimeService;

    private readonly PercentMoveTickerStreamFactory _percentMoveTickerStreamFactory;

    public PercentMovePositionWorker(
        ILogger<PercentMovePositionWorker> logger,
        IThRestBinanceClient restBinanceClient, 
        IThSocketBinanceClient socketBinanceClient,
        IDateTimeService dateTimeService, 
        PercentMoveTickerStreamFactory percentMoveTickerStreamFactory
        )
    {
        _logger = logger;
        _restBinanceClient = restBinanceClient;
        _socketBinanceClient = socketBinanceClient;
        _dateTimeService = dateTimeService;
        _percentMoveTickerStreamFactory = percentMoveTickerStreamFactory;
    }

    public override async Task<ActionResult> CreatePositionAsync(ITradeLogicStore tradeLogicStore, string symbol, PositionSide side, decimal entryPrice, 
        DateTime lastUpdateTime, decimal quantity, bool isPositionExist, CancellationToken cancellationToken)
    {
        try
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Cancellation token is requested. In {Method}", 
                    nameof(CreatePositionAsync));

                return ActionResult.CancellationTokenRequested;
            }

            var lastOrderPrice = entryPrice;
            
            if (isPositionExist)
            {
                var lastOrdersRequest = await _restBinanceClient.UsdFuturesApi.Trading.GetOrdersAsync(
                    symbol,
                    startTime: _dateTimeService.GetUtcDateTime().AddDays(-2),
                    endTime: _dateTimeService.GetUtcDateTime(),
                    ct: cancellationToken
                );
                
                if (!lastOrdersRequest.Success)
                {
                    _logger.LogError(new ThException(lastOrdersRequest.Error), "{Position} | {Side}. In {Method}",
                        symbol, side, nameof(CreatePositionAsync));

                    return ActionResult.ClientError;
                }

                lastOrderPrice = lastOrdersRequest.Data.Any() ? lastOrdersRequest.Data.Last().AvgPrice : entryPrice;   
            }

            var pmsStore = (PercentMoveStore)tradeLogicStore;
            
            var symbolInfo = pmsStore.FuturesUsd.ExchangerData.ExchangeInfo.Symbols.Single(x => x.Name == symbol);
            var position = pmsStore.FuturesUsd.AccountData.Positions.First(x => x.Symbol == symbol);
            
            var openedPosition = new Position
            {
                Name = symbol,
                BaseAsset = symbolInfo.BaseAsset,
                QuoteAsset = symbolInfo.QuoteAsset,
                PositionSide = side,
                EntryPrice = entryPrice,
                LastUpdateTime = lastUpdateTime,
                TotalQuantity = Math.Abs(quantity),
                Leverage = position.Leverage
            };
            
            pmsStore.Positions.Add(openedPosition);

            if (!pmsStore.SymbolStatus.ContainsKey(symbol))
            {
                pmsStore.SymbolStatus.Add(symbol, true);    
            }
            
            AddOrUpdateOrderPrice(pmsStore, symbol, lastOrderPrice);

            if (!pmsStore.UsdFuturesTickerStreams.ContainsKey(symbol))
            {
                var stream = _percentMoveTickerStreamFactory.GetPmsSymbolTickerStream();
                await stream.StartStreamSymbolTickerAsync(symbol, cancellationToken: cancellationToken);
                pmsStore.UsdFuturesTickerStreams.Add(symbol, stream);
            }
            
            _logger.LogInformation("{Position}. Position created. In {Method}", 
                openedPosition.ToString(), nameof(CreatePositionAsync));

            return ActionResult.Success;
        }
        catch (TaskCanceledException taskCanceledException)
        {
            _logger.LogWarning("{Message}. In {Method}",
                taskCanceledException.Message, nameof(CreatePositionAsync));
            
            return ActionResult.CancellationTokenRequested;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "{Symbol} | {Side} | Entry price {EntryPrice} | Quantity {Quantity}. In {Method}", 
                symbol, side, entryPrice, quantity, nameof(CreatePositionAsync));
            
            return ActionResult.SystemError;
        }
    }

    public override ActionResult UpdatePositionDetails(ITradeLogicStore tradeLogicStore, Position openedPosition, 
        BinancePositionDetailsUsdt positionDetails)
    {
        try
        {
            openedPosition.EntryPrice = positionDetails.EntryPrice;
            openedPosition.TotalQuantity = Math.Abs(positionDetails.Quantity);
            openedPosition.Leverage = positionDetails.Leverage;

            return ActionResult.Success;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "{Position}. In {Method}", 
                openedPosition.ToString(), nameof(UpdatePositionDetails));
            
            return ActionResult.SystemError;
        }
    }
    
    public void UpdatePositionQuantity(ITradeLogicStore tradeLogicStore, Position openedPosition, 
        BinanceFuturesStreamOrderUpdate orderUpdate, bool isWithdraw)
    {
        try
        {
            if (isWithdraw)
            {
                openedPosition.TotalQuantity -= orderUpdate.UpdateData.QuantityOfLastFilledTrade;   
            
                _logger.LogInformation("Update Quantity: -{Quantity}. In {Method}", 
                    orderUpdate.UpdateData.Quantity, nameof(UpdatePositionQuantity));
            }
            else
            {
                openedPosition.TotalQuantity += orderUpdate.UpdateData.QuantityOfLastFilledTrade;   
            
                _logger.LogInformation("Update Quantity: {Quantity}. In {Method}", 
                    orderUpdate.UpdateData.Quantity, nameof(UpdatePositionQuantity));
            }

            AddOrUpdateOrderPrice((PercentMoveStore)tradeLogicStore, openedPosition.Name, orderUpdate.UpdateData.AveragePrice);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "{Position}. In {Method}", 
                openedPosition.ToString(), nameof(UpdatePositionQuantity));
        }
    }
    
    public override async Task<ActionResult> DeletePositionAsync(ITradeLogicStore tradeLogicStore, Position positionToDelete, 
        CancellationToken cancellationToken)
    {
        var positionInString = positionToDelete.ToString();
        
        try
        {
            var pmsStore = (PercentMoveStore)tradeLogicStore;
            
            if (pmsStore.Positions.Count(x => x.Name == positionToDelete.Name) == 1)
            {
                pmsStore.MarketLastPrices.Remove(positionToDelete.Name);

                pmsStore.SymbolStatus.Remove(positionToDelete.Name);
                
                var stream = pmsStore.UsdFuturesTickerStreams[positionToDelete.Name];
                await _socketBinanceClient.UnsubscribeAsync(stream.SocketSubscription);
                pmsStore.UsdFuturesTickerStreams.Remove(positionToDelete.Name);
                
                _logger.LogInformation("{Position}. Unsubscribed from socket. In {Method}", 
                    positionInString, nameof(DeletePositionAsync));
            }

            pmsStore.Positions.Remove(positionToDelete);
            
            _logger.LogInformation("{Position}. Position removed. In {Method}", 
                positionInString, nameof(DeletePositionAsync));

            return ActionResult.Success;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "{Position}. In {Method}", 
                positionInString, nameof(DeletePositionAsync));
            
            return ActionResult.SystemError;
        }
    }

    #region Private methods

    private void AddOrUpdateOrderPrice(PercentMoveStore percentMoveStore, string symbol, decimal lastOrderPrice)
    {
        try
        {
            if (percentMoveStore.SymbolLastOrderPrice.ContainsKey(symbol))
            {
                percentMoveStore.SymbolLastOrderPrice[symbol] = lastOrderPrice;
            }
            else
            {
                percentMoveStore.SymbolLastOrderPrice.Add(symbol, lastOrderPrice);   
            }
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "{Symbol}. In {Method}", 
                symbol, nameof(AddOrUpdateOrderPrice));
        }
    }

    #endregion
}