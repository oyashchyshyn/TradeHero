using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures;
using Microsoft.Extensions.Logging;
using TradeHero.Core.Enums;
using TradeHero.Core.Exceptions;
using TradeHero.Core.Types.Client;
using TradeHero.Core.Types.Services;
using TradeHero.Core.Types.Services.Models.Calculator;
using TradeHero.Core.Types.Trading.Models;
using TradeHero.Core.Types.Trading.Models.Instance;
using TradeHero.Trading.Endpoints.Rest;
using TradeHero.Trading.Logic.PercentLimit.Options;

namespace TradeHero.Trading.Logic.PercentLimit.Flow;

internal class  PercentLimitEndpoints
{
    private readonly ILogger<PercentLimitEndpoints> _logger;
    private readonly IThRestBinanceClient _restBinanceClient;
    private readonly ICalculatorService _calculatorService;
    private readonly IFuturesUsdEndpoints _futuresUsdEndpoints;

    public PercentLimitEndpoints(
        ILogger<PercentLimitEndpoints> logger,
        IThRestBinanceClient restBinanceClient,
        ICalculatorService calculatorService,
        IFuturesUsdEndpoints futuresUsdEndpoints
        )
    {
        _logger = logger;
        _restBinanceClient = restBinanceClient;
        _calculatorService = calculatorService;
        _futuresUsdEndpoints = futuresUsdEndpoints;
    }

    public async Task<ActionResult> CreateBuyMarketOrderAsync(SymbolMarketInfo symbolMarketInfo, decimal lastPrice, BinanceFuturesUsdtSymbol symbolInfo, 
        BinancePositionDetailsUsdt positionInfo, BinanceFuturesAccountBalance balance, PercentLimitTradeLogicLogicOptions tradeLogicLogicOptions, int maxRetries = 5, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("{Symbol} | {Side}. Start create open position market order. In {Method}",
                symbolMarketInfo.FuturesUsdName, symbolMarketInfo.PositionSide, nameof(CreateBuyMarketOrderAsync));

            if (symbolInfo.MinNotionalFilter == null)
            {
                _logger.LogError("{Symbol} | {Side}. {Filter} is null. In {Method}",
                    symbolMarketInfo.FuturesUsdName, symbolMarketInfo.PositionSide,
                    nameof(symbolInfo.MinNotionalFilter), nameof(CreateBuyMarketOrderAsync));

                return ActionResult.Error;
            }
            
            if (symbolInfo.LotSizeFilter == null)
            {
                _logger.LogError("{Symbol} | {Side}. {Filter} is null. In {Method}",
                    symbolMarketInfo.FuturesUsdName, symbolMarketInfo.PositionSide,
                    nameof(symbolInfo.LotSizeFilter), nameof(CreateBuyMarketOrderAsync));
                
                return ActionResult.Error;
            }

            var initialMargin = tradeLogicLogicOptions.PercentFromDepositForOpen != 0 
                ? Math.Round(balance.AvailableBalance * tradeLogicLogicOptions.PercentFromDepositForOpen / 100, 2) * positionInfo.Leverage
                : symbolInfo.MinNotionalFilter.MinNotional; 
                
            _logger.LogInformation("{Symbol} | {Side}. Percent from deposit is {Percent}%. Margin with leverage is {Margin}. In {Method}",
                symbolMarketInfo.FuturesUsdName, symbolMarketInfo.PositionSide, tradeLogicLogicOptions.PercentFromDepositForOpen, 
                initialMargin, nameof(CreateBuyMarketOrderAsync));

            var orderQuantity = _calculatorService.GetOrderQuantity(
                lastPrice,
                initialMargin,
                symbolInfo.LotSizeFilter.MinQuantity
            );

            var marginForOrder = orderQuantity * lastPrice / positionInfo.Leverage;
            
            _logger.LogInformation("{Symbol} | {Side}. Future position quantity: {Quantity}. Last price: {LastPrice}. Margin for order: {Margin}. In {Method}",
                symbolMarketInfo.FuturesUsdName, symbolMarketInfo.PositionSide, orderQuantity, lastPrice, marginForOrder,
                nameof(CreateBuyMarketOrderAsync));
            
            if (balance.WalletBalance <= 0)
            {
                _logger.LogWarning("{Symbol} | {Side}. Wallet balance is lower or equal to zero. In {Method}",
                    symbolMarketInfo.FuturesUsdName, symbolMarketInfo.PositionSide, nameof(CreateMarketAverageBuyOrderAsync));
                
                return ActionResult.Error;
            }
            
            var availableBalancePercent = _calculatorService.GetAvailableBalancePercentWithFutureMargin(
                balance.WalletBalance, 
                balance.AvailableBalance,
                marginForOrder
            );
            
            if (100.0m - availableBalancePercent > tradeLogicLogicOptions.AvailableDepositPercentForTrading)
            {
                _logger.LogWarning("{Symbol} | {Side}. Margin is not available. Margin percent for trading is {MarginPercentIsSettings}. " +
                                "Current balance percent in use with future order is {BalancePercentInUse}. " +
                                "Current balance: {Balance}. Available balance: {AvailableBalance}. In {Method}",
                    symbolMarketInfo.FuturesUsdName, symbolMarketInfo.PositionSide, 
                    tradeLogicLogicOptions.AvailableDepositPercentForTrading, 100.0m - availableBalancePercent, 
                    balance.WalletBalance, balance.AvailableBalance, nameof(CreateMarketAverageBuyOrderAsync));
                
                return ActionResult.Error;
            }

            var isNeedToUpdatePrice = false;
            
            foreach (var futureOrderQuantity in _calculatorService.SplitPositionQuantity(orderQuantity, symbolInfo.LotSizeFilter.MaxQuantity))
            {
                var currentOrderQuantity = futureOrderQuantity;
                
                for (var i = 0; i < maxRetries; i++)
                { 
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Cancellation token is requested. In {Method}", 
                            nameof(CreateBuyMarketOrderAsync));

                        return ActionResult.CancellationTokenRequested;
                    }

                    if (isNeedToUpdatePrice)
                    {
                        var lastPriceRequest = await _restBinanceClient.UsdFuturesApi.ExchangeData.GetPriceAsync(
                            symbolMarketInfo.FuturesUsdName,
                            ct: cancellationToken
                        );

                        if (!lastPriceRequest.Success)
                        {
                            _logger.LogWarning(new ThException(lastPriceRequest.Error),"{Symbol} | {Side}. In {Method}",
                                symbolMarketInfo.FuturesUsdName, symbolMarketInfo.PositionSide, nameof(CreateBuyMarketOrderAsync));
                            
                            continue;
                        }
                        
                        currentOrderQuantity = _calculatorService.GetOrderQuantity(
                            lastPriceRequest.Data.Price,
                            initialMargin,
                            symbolInfo.LotSizeFilter.MinQuantity
                        );

                        isNeedToUpdatePrice = false;
                    }
                    
                    var placeOrderRequest = await _restBinanceClient.UsdFuturesApi.Trading.PlaceOrderAsync(
                        symbol: symbolMarketInfo.FuturesUsdName,
                        side: symbolMarketInfo.PositionSide == PositionSide.Short ? OrderSide.Sell : OrderSide.Buy,
                        type: FuturesOrderType.Market,
                        quantity: currentOrderQuantity,
                        positionSide: symbolMarketInfo.PositionSide,
                        ct: cancellationToken
                    );

                    if (placeOrderRequest.Success)
                    {
                        _logger.LogInformation("{Symbol} | {Side}. Open position market order with id {OrderId} and quantity {Quantity} placed successfully. In {Method}",
                            symbolMarketInfo.FuturesUsdName, symbolMarketInfo.PositionSide, placeOrderRequest.Data.Id, 
                            currentOrderQuantity, nameof(CreateBuyMarketOrderAsync)); 
                        
                        break;
                    }

                    _logger.LogWarning(new ThException(placeOrderRequest.Error),"{Symbol} | {Side}. In {Method}",
                        symbolMarketInfo.FuturesUsdName, symbolMarketInfo.PositionSide, nameof(CreateBuyMarketOrderAsync));

                    switch (placeOrderRequest.Error?.Code)
                    {
                        case (int)ApiErrorCodes.MinNotionalError:
                        {
                            isNeedToUpdatePrice = true;
                            
                            continue;
                        }
                        case (int)ApiErrorCodes.MaximumExceededAtCurrentLeverage:
                        {
                            _logger.LogError("{Symbol} | {Side}. Order won't be placed due to last warning. In {Method}",
                                symbolMarketInfo.FuturesUsdName, symbolMarketInfo.PositionSide, nameof(CreateBuyMarketOrderAsync));
                        
                            return ActionResult.ClientError;
                        }
                    }

                    if (i != maxRetries - 1)
                    {
                        continue;
                    }
                    
                    _logger.LogError("{Symbol} | {Side}. {Number} retries exceeded In {Method}",
                        symbolMarketInfo.FuturesUsdName, symbolMarketInfo.PositionSide, maxRetries, nameof(CreateBuyMarketOrderAsync));

                    return ActionResult.ClientError;
                }
            }

            return ActionResult.Success;
        }
        catch (TaskCanceledException taskCanceledException)
        {
            _logger.LogInformation("{Message}. In {Method}",
                taskCanceledException.Message, nameof(CreateBuyMarketOrderAsync));

            return ActionResult.CancellationTokenRequested;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "{Symbol} | {Side}. In {Method}",
                symbolMarketInfo.FuturesUsdName, symbolMarketInfo.PositionSide, nameof(CreateBuyMarketOrderAsync));

            return ActionResult.SystemError;
        }
    } 
    
    public async Task<ActionResult> CreateMarketAverageBuyOrderAsync(Position openedPosition, decimal lastPrice, BinanceFuturesUsdtSymbol symbolInfo, 
        BinanceFuturesAccountBalance balance, PercentLimitTradeLogicLogicOptions tradeLogicLogicOptions, int maxRetries = 5, CancellationToken cancellationToken = default)
    {
        var positionString = openedPosition.ToString();

        try
        {
            _logger.LogInformation("{Position}. Start create average market buy order. In {Method}",
                positionString, nameof(CreateMarketAverageBuyOrderAsync));

            if (symbolInfo.MinNotionalFilter == null)
            {
                _logger.LogError("{Position}. {Filter} is null. In {Method}",
                    positionString, nameof(symbolInfo.MinNotionalFilter), nameof(CreateMarketAverageBuyOrderAsync));
                
                return ActionResult.Error;
            }
            
            if (symbolInfo.LotSizeFilter == null)
            {
                _logger.LogError("{Position}. {Filter} is null. In {Method}",
                    positionString, nameof(symbolInfo.LotSizeFilter), nameof(CreateMarketAverageBuyOrderAsync));
                
                return ActionResult.Error;
            }

            var orderQuantity = _calculatorService.CalculateOrderQuantity(new CalculatedOrderQuantity
            {
                MinNotional = symbolInfo.MinNotionalFilter.MinNotional,
                MinOrderSize = symbolInfo.LotSizeFilter.MinQuantity,
                Side = openedPosition.PositionSide,
                EntryPrice = openedPosition.EntryPrice,
                TotalQuantity = openedPosition.TotalQuantity,
                Leverage = openedPosition.Leverage,
                LastPrice = lastPrice,
                MinRoePercent = tradeLogicLogicOptions.AverageToRoe
            });
            
            if (orderQuantity <= 0)
            {
                _logger.LogError("{Position}. Quantity is {Quantity}. In {Method}",
                    positionString, orderQuantity, nameof(CreateMarketAverageBuyOrderAsync));

                return ActionResult.Error;
            }
            
            var marginForOrder = orderQuantity * lastPrice / openedPosition.Leverage;
                    
            _logger.LogInformation("{Position}. Future order quantity: {Quantity}. Last price: {LastPrice}. Margin for order: {Margin}. In {Method}",
                positionString, orderQuantity, lastPrice, marginForOrder, nameof(CreateBuyMarketOrderAsync));
                    
            if (balance.WalletBalance <= 0)
            {
                _logger.LogWarning("{Position}. Wallet balance is lower or equal to zero. In {Method}",
                    positionString, nameof(CreateMarketAverageBuyOrderAsync));
                        
                return ActionResult.Error;
            }
                    
            var availableBalancePercent = _calculatorService.GetAvailableBalancePercentWithFutureMargin(
                balance.WalletBalance, 
                balance.AvailableBalance,
                marginForOrder
            );
                    
            if (100.0m - availableBalancePercent > tradeLogicLogicOptions.AvailableDepositPercentForTrading)
            {
                _logger.LogWarning("{Position}. Margin is not available. Margin percent for trading is {MarginPercentIsSettings}." +
                                   "Current balance percent in use with future order is {BalancePercentInUse}. " +
                                   "Current balance: {Balance}. Available balance: {AvailableBalance}. In {Method}",
                    positionString, tradeLogicLogicOptions.AvailableDepositPercentForTrading, 100.0m - availableBalancePercent, 
                    balance.WalletBalance, balance.AvailableBalance, nameof(CreateMarketAverageBuyOrderAsync));
                        
                return ActionResult.Error;
            }
            
            foreach (var futureOrderQuantity in _calculatorService.SplitPositionQuantity(orderQuantity, symbolInfo.LotSizeFilter.MaxQuantity))
            {
                for (var i = 0; i < maxRetries; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Cancellation token is requested. In {Method}", 
                            nameof(CreateMarketAverageBuyOrderAsync));

                        return ActionResult.CancellationTokenRequested;
                    }

                    var placeOrderRequest = await _restBinanceClient.UsdFuturesApi.Trading.PlaceOrderAsync(
                        symbol: openedPosition.Name,
                        side: openedPosition.PositionSide == PositionSide.Short ? OrderSide.Sell : OrderSide.Buy,
                        type: FuturesOrderType.Market,
                        quantity: futureOrderQuantity,
                        positionSide: openedPosition.PositionSide,
                        ct: cancellationToken
                    );

                    if (placeOrderRequest.Success)
                    {
                        _logger.LogInformation("{Position}. Average market order with id {OrderId} and quantity {Quantity} placed successfully. In {Method}",
                            positionString, placeOrderRequest.Data.Id, futureOrderQuantity, nameof(CreateMarketAverageBuyOrderAsync));

                        break;
                    }

                    _logger.LogWarning(new ThException(placeOrderRequest.Error),"{Position}. In {Method}",
                        positionString, nameof(CreateMarketAverageBuyOrderAsync));

                    if (i != maxRetries - 1 && (ApiErrorCodes)(placeOrderRequest.Error?.Code ?? 0) == ApiErrorCodes.MaximumExceededAtCurrentLeverage)
                    {
                        await _futuresUsdEndpoints.ChangeSymbolLeverageToAvailableAsync(openedPosition.Name, cancellationToken: cancellationToken);
                    }
                    
                    if (i != maxRetries - 1)
                    {
                        continue;
                    }
                    
                    _logger.LogError("{Position}. {Number} retries exceeded In {Method}",
                        positionString, maxRetries, nameof(CreateMarketAverageBuyOrderAsync));

                    return ActionResult.ClientError;
                }   
            }

            return ActionResult.Success;
        }
        catch (TaskCanceledException taskCanceledException)
        {
            _logger.LogInformation("{Message}. In {Method}",
                taskCanceledException.Message, nameof(CreateMarketAverageBuyOrderAsync));

            return ActionResult.CancellationTokenRequested;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "{Position}. In {Method}",
                positionString, nameof(CreateMarketAverageBuyOrderAsync));

            return ActionResult.SystemError;
        }
    }

    public async Task<ActionResult> CreateMarketStopOrderAsync(Position openedPosition, decimal lastPrice, decimal? percentFromLastPrice, 
        BinanceFuturesUsdtSymbol symbolInfo, int maxRetries = 5, CancellationToken cancellationToken = default)
    {
        var positionString = openedPosition.ToString();

        try
        {
            if (!percentFromLastPrice.HasValue)
            {
                return ActionResult.Success;
            }
            
            _logger.LogInformation("{Position}. Start create stop loss order. In {Method}",
                positionString, nameof(CreateMarketStopOrderAsync));

            if (symbolInfo.PriceFilter == null)
            {
                _logger.LogError("{Position}. {Filter} is null. In {Method}",
                    positionString, nameof(symbolInfo.PriceFilter), nameof(CreateMarketStopOrderAsync));
                
                return ActionResult.Error;
            }
            
            if (symbolInfo.LotSizeFilter == null)
            {
                _logger.LogError("{Position}. {Filter} is null. In {Method}",
                    positionString, nameof(symbolInfo.LotSizeFilter), nameof(CreateMarketStopOrderAsync));
                
                return ActionResult.Error;
            }

            if (percentFromLastPrice == 0)
            {
                _logger.LogError("{Position}. {Property} is zero. In {Method}",
                    positionString, nameof(percentFromLastPrice), 
                    nameof(CreateMarketStopOrderAsync));
                
                return ActionResult.Error;
            }

            decimal stopLimitPriceFromLastPricePercent;
            if (openedPosition.PositionSide == PositionSide.Short)
            {
                stopLimitPriceFromLastPricePercent = percentFromLastPrice.Value > 0
                    ? percentFromLastPrice.Value
                    : -percentFromLastPrice.Value;
            }
            else
            {
                stopLimitPriceFromLastPricePercent = percentFromLastPrice.Value > 0
                    ? -percentFromLastPrice.Value
                    : percentFromLastPrice.Value;
            }

            foreach (var partOfOrderQuantity in _calculatorService.SplitPositionQuantity(openedPosition.TotalQuantity, symbolInfo.LotSizeFilter.MaxQuantity))
            {
                for (var i = 0; i < maxRetries; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Cancellation token is requested. In {Method}", 
                            nameof(CreateMarketStopOrderAsync));

                        return ActionResult.CancellationTokenRequested;
                    }

                    var stopPrice = _calculatorService.GetPriceFromPercent(
                        lastPrice,
                        stopLimitPriceFromLastPricePercent
                    );

                    var roundedStopPrice = _calculatorService.RoundToSize(stopPrice, symbolInfo.PriceFilter.TickSize);
                    
                    _logger.LogInformation("{Position}. Last price: {LastPrice}. Stop price: {StopPrice}. Rounded Stop price: {RoundedStopPrice}. " +
                                           "Tick size: {TickSize}. Activation percent: {ActivationPercent}. In {Method}",
                        positionString, lastPrice, stopPrice, roundedStopPrice, symbolInfo.PriceFilter.TickSize, stopLimitPriceFromLastPricePercent, 
                        nameof(CreateMarketStopOrderAsync));

                    var side = openedPosition.PositionSide == PositionSide.Short ? OrderSide.Buy : OrderSide.Sell;
                    
                    var placeOrderRequest = await _restBinanceClient.UsdFuturesApi.Trading.PlaceOrderAsync(
                        symbol: openedPosition.Name,
                        side: side,
                        type: FuturesOrderType.StopMarket,
                        quantity: partOfOrderQuantity,
                        positionSide: openedPosition.PositionSide,
                        stopPrice: roundedStopPrice,
                        closePosition: true,
                        timeInForce: TimeInForce.GoodTillCanceled,
                        ct: cancellationToken
                    );

                    if (placeOrderRequest.Success)
                    {
                        _logger.LogInformation("{Position}. Stop market order with id {OrderId} and quantity {Quantity} placed successfully. In {Method}",
                            positionString, placeOrderRequest.Data.Id, partOfOrderQuantity, nameof(CreateMarketAverageBuyOrderAsync));
                        
                        _logger.LogInformation("{Position}. Stop market order placed successfully. In {Method}",
                            positionString, nameof(CreateMarketStopOrderAsync));
                        
                        break;
                    }

                    _logger.LogWarning(new ThException(placeOrderRequest.Error),"{Position}. In {Method}",
                        positionString, nameof(CreateMarketStopOrderAsync));

                    if ((ApiErrorCodes)(placeOrderRequest.Error?.Code ?? 0) == ApiErrorCodes.WouldTriggerImmediately)
                    {
                        var placeMarketOrderRequest = await _restBinanceClient.UsdFuturesApi.Trading.PlaceOrderAsync(
                            symbol: openedPosition.Name,
                            side: side,
                            type: FuturesOrderType.Market,
                            quantity: openedPosition.TotalQuantity,
                            positionSide: openedPosition.PositionSide,
                            ct: cancellationToken
                        );

                        if (!placeOrderRequest.Success)
                        {
                            _logger.LogWarning(new ThException(placeMarketOrderRequest.Error),"{Position}. Market order request. In {Method}",
                                positionString, nameof(CreateMarketStopOrderAsync));
                            
                            continue;
                        }
                        
                        _logger.LogInformation("{Position}. Market order placed successfully. Market order in {Method}",
                            positionString, nameof(CreateMarketStopOrderAsync));

                        return ActionResult.Success;
                    }
                    
                    if (i != maxRetries - 1)
                    {
                        continue;
                    }
                    
                    _logger.LogError("{Position}. {Number} retries exceeded In {Method}",
                        positionString, maxRetries, nameof(CreateMarketStopOrderAsync));

                    return ActionResult.ClientError;
                }   
            }

            return ActionResult.Success;
        }
        catch (TaskCanceledException taskCanceledException)
        {
            _logger.LogWarning("{Message}. In {Method}",
                taskCanceledException.Message, nameof(CreateMarketStopOrderAsync));

            return ActionResult.CancellationTokenRequested;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "{Position}. In {Method}",
                positionString, nameof(CreateMarketStopOrderAsync));

            return ActionResult.SystemError;
        }
    }
    
    public async Task<ActionResult> CreateMarketClosePositionOrderAsync(Position openedPosition, BinanceFuturesUsdtSymbol symbolInfo, 
        int maxRetries = 5, CancellationToken cancellationToken = default)
    {
        var positionString = openedPosition.ToString();
        
        try
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Cancellation token is requested. In {Method}", 
                    nameof(CreateMarketClosePositionOrderAsync));

                return ActionResult.CancellationTokenRequested;
            }

            if (symbolInfo.LotSizeFilter == null)
            {
                _logger.LogError("{Position}. {Filter} is null. In {Method}",
                    positionString, nameof(symbolInfo.LotSizeFilter), nameof(CreateMarketClosePositionOrderAsync));
                
                return ActionResult.Error;
            }

            foreach (var orderQuantityToClose in _calculatorService.SplitPositionQuantity(openedPosition.TotalQuantity, symbolInfo.LotSizeFilter.MaxQuantity))
            {
                for (var i = 0; i < maxRetries; i++)
                {
                    if (openedPosition.TotalQuantity == 0)
                    {
                        return ActionResult.Success;
                    }
                
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Cancellation token is requested. In {Method}", 
                            nameof(CreateMarketClosePositionOrderAsync));

                        return ActionResult.CancellationTokenRequested;
                    }

                    var placeOrderRequest = await _restBinanceClient.UsdFuturesApi.Trading.PlaceOrderAsync(
                        symbol: openedPosition.Name,
                        side: openedPosition.PositionSide == PositionSide.Short ? OrderSide.Buy : OrderSide.Sell,
                        type: FuturesOrderType.Market,
                        quantity: orderQuantityToClose,
                        positionSide: openedPosition.PositionSide,
                        ct: cancellationToken
                    );

                    if (placeOrderRequest.Success)
                    {
                        _logger.LogInformation("{Position}. Close market order with id {OrderId} and quantity {Quantity} placed successfully. In {Method}",
                            positionString, placeOrderRequest.Data.Id, orderQuantityToClose, nameof(CreateMarketAverageBuyOrderAsync));
                        
                        break;
                    }

                    _logger.LogWarning(new ThException(placeOrderRequest.Error),"{Position}. In {Method}",
                        positionString, nameof(CreateMarketClosePositionOrderAsync));

                    if (i != maxRetries - 1)
                    {
                        continue;
                    }
                
                    _logger.LogError("{Position}. {Number} retries exceeded In {Method}",
                        positionString, maxRetries, nameof(CreateMarketClosePositionOrderAsync));

                    return ActionResult.ClientError;
                }   
            }

            return ActionResult.Success;
        }
        catch (TaskCanceledException taskCanceledException)
        {
            _logger.LogWarning("{Message}. In {Method}",
                taskCanceledException.Message, nameof(CreateMarketClosePositionOrderAsync));

            return ActionResult.CancellationTokenRequested;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "{Position}. In {Method}",
                positionString, nameof(CreateMarketClosePositionOrderAsync));

            return ActionResult.SystemError;
        }
    }
}