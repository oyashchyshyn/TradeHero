using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures;
using Binance.Net.Objects.Models.Futures.Socket;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Base.Exceptions;
using TradeHero.Contracts.Client;
using TradeHero.Contracts.Services;
using TradeHero.Contracts.Strategy;
using TradeHero.Contracts.Strategy.Models;
using TradeHero.Strategies.Base;
using TradeHero.Strategies.Endpoints.Rest;
using TradeHero.Strategies.Strategies.PercentLimitsStrategy.Factory;
using TradeHero.Strategies.Strategies.PercentLimitsStrategy.Models;

namespace TradeHero.Strategies.Strategies.PercentLimitsStrategy.Flow;

internal class PlsPositionWorker : BasePositionWorker
{
    private readonly ILogger<PlsPositionWorker> _logger;
    private readonly IThRestBinanceClient _restBinanceClient;
    private readonly IThSocketBinanceClient _socketBinanceClient;
    private readonly IFuturesUsdEndpoints _futuresUsdEndpoints;
    private readonly IDateTimeService _dateTimeService;

    private readonly PlsSymbolTickerStreamFactory _plsSymbolTickerStreamFactory;
    
    public PlsPositionWorker(
        ILogger<PlsPositionWorker> logger,
        IThRestBinanceClient restBinanceClient,
        IThSocketBinanceClient socketBinanceClient,
        IFuturesUsdEndpoints futuresUsdEndpoints,
        IDateTimeService dateTimeService, 
        PlsSymbolTickerStreamFactory plsSymbolTickerStreamFactory
        )
    {
        _logger = logger;
        _restBinanceClient = restBinanceClient;
        _socketBinanceClient = socketBinanceClient;
        _futuresUsdEndpoints = futuresUsdEndpoints;
        _dateTimeService = dateTimeService;
        _plsSymbolTickerStreamFactory = plsSymbolTickerStreamFactory;
    }

    public override async Task<ActionResult> CreatePositionAsync(IStrategyStore strategyStore, string symbol, PositionSide side, decimal entryPrice, DateTime lastUpdateTime,
        decimal quantity, bool isPositionExist, CancellationToken cancellationToken)
    {
        try
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Cancellation token is requested. In {Method}", 
                    nameof(CreatePositionAsync));

                return ActionResult.CancellationTokenRequested;
            }

            var newOrderContainer = new PositionInfo
            {
                IsNeedToCheckPosition = true,
                IsNeedToPlaceMarketStop = true
            };
            
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

                if (lastOrdersRequest.Data.Any(x => x.Status == OrderStatus.New))
                {
                    foreach (var futuresOrder in lastOrdersRequest.Data.Where(x => x.Status == OrderStatus.New))
                    {
                        switch (futuresOrder.Type)
                        {
                            case FuturesOrderType.StopMarket:
                                newOrderContainer.IsNeedToPlaceMarketStop = false;
                                _logger.LogInformation("{Position} | {Side}. Stop market applied. In {Method}",
                                    symbol, side, nameof(CreatePositionAsync));
                                break;
                        }
                    }   
                }
                else
                {
                    _logger.LogInformation("{Position} | {Side}. There is no opened orders. In {Method}",
                        symbol, side, nameof(CreatePositionAsync));
                }   
            }

            var plsStore = (PlsStore)strategyStore;
            
            var symbolInfo = plsStore.FuturesUsd.ExchangerData.ExchangeInfo.Symbols.Single(x => x.Name == symbol);
            var position = plsStore.FuturesUsd.AccountData.Positions.First(x => x.Symbol == symbol);
            
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

            plsStore.Positions.Add(openedPosition);
            plsStore.PositionsInfo.Add($"{symbol}_{side}", newOrderContainer);

            if (!plsStore.UsdFuturesTickerStreams.ContainsKey(symbol))
            {
                var stream = _plsSymbolTickerStreamFactory.GetPlsSymbolTickerStream();
                await stream.StartStreamSymbolTickerAsync(symbol, cancellationToken: cancellationToken);
                plsStore.UsdFuturesTickerStreams.Add(symbol, stream);
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
            _logger.LogCritical(exception, "{Symbol} | {Side} | Entry price: {EntryPrice} | Quantity: {Quantity}. In {Method}", 
                symbol, side, entryPrice, quantity, nameof(CreatePositionAsync));
            
            return ActionResult.SystemError;
        }
    }
    
    public ActionResult UpdatePositionQuantity(Position openedPosition, BinanceFuturesStreamOrderUpdate orderUpdate, bool isWithdrawQuantity)
    {
        try
        {
            if (isWithdrawQuantity)
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

            return ActionResult.Success;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "{Position}. In {Method}", 
                openedPosition.ToString(), nameof(UpdatePositionQuantity));
            
            return ActionResult.SystemError;
        }
    }
    
    public override ActionResult UpdatePositionDetails(IStrategyStore strategyStore, Position openedPosition, BinancePositionDetailsUsdt positionDetails)
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
    
    public override async Task<ActionResult> DeletePositionAsync(IStrategyStore strategyStore, Position openedPosition, CancellationToken cancellationToken)
    {
        var openedPositionString = openedPosition.ToString();
        
        try
        {
            var plsStore = (PlsStore)strategyStore;
         
            plsStore.PositionsInfo.Remove($"{openedPosition.Name}_{openedPosition.PositionSide}");
            
            if (plsStore.Positions.Count(x => x.Name == openedPosition.Name) == 1)
            {
                var stream = plsStore.UsdFuturesTickerStreams[openedPosition.Name];
                await _socketBinanceClient.UnsubscribeAsync(stream.SocketSubscription);
                plsStore.UsdFuturesTickerStreams.Remove(openedPosition.Name);
                
                _logger.LogInformation("{Position}. Unsubscribed from socket. In {Method}", 
                    openedPositionString, nameof(DeletePositionAsync));
            }

            await _futuresUsdEndpoints.CancelOpenedOrdersAsync(
                openedPosition.Name, 
                openedPosition.PositionSide, 
                cancellationToken: cancellationToken
            );
            
            plsStore.Positions.Remove(openedPosition);
            
            _logger.LogInformation("{Position}. Position Removed. In {Method}", 
                openedPositionString, nameof(DeletePositionAsync));
            
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
            _logger.LogCritical(exception, "{Position}. In {Method}", 
                openedPositionString, nameof(DeletePositionAsync));
            
            return ActionResult.SystemError;
        }
    }
}