using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures;
using Microsoft.Extensions.Logging;
using TradeHero.Core.Contracts.Client;
using TradeHero.Core.Contracts.Services;
using TradeHero.Core.Enums;
using TradeHero.Core.Exceptions;
using TradeHero.Core.Models.Trading;
using TradeHero.Trading.Endpoints.Rest;

namespace TradeHero.Trading.Logic.PercentMove.Flow;

internal class PercentMoveEndpoints
{
    private readonly ILogger<PercentMoveEndpoints> _logger;
    private readonly IThRestBinanceClient _restBinanceClient;
    private readonly ICalculatorService _calculatorService;
    private readonly IFuturesUsdEndpoints _futuresUsdEndpoints;

    public PercentMoveEndpoints(
        ILogger<PercentMoveEndpoints> logger,
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
    
    public async Task<ActionResult> CreateBuyMarketOrderAsync(Position openedPosition, BinanceFuturesUsdtSymbol symbolInfo, 
        BinanceFuturesAccountBalance balance, int maxRetries = 5, CancellationToken cancellationToken = default)
    {
        var positionString = openedPosition.ToString();
        
        try
        {
            _logger.LogInformation("{Position}. Start create buy order. In {Method}",
                positionString, nameof(CreateBuyMarketOrderAsync));

            if (symbolInfo.MinNotionalFilter == null)
            {
                _logger.LogError("{Position}. {Filter} is null. In {Method}",
                    openedPosition, nameof(symbolInfo.MinNotionalFilter), nameof(CreateBuyMarketOrderAsync));
                
                return ActionResult.Error;
            }
            
            if (symbolInfo.LotSizeFilter == null)
            {
                _logger.LogError("{Position}. {Filter} is null. In {Method}",
                    openedPosition, nameof(symbolInfo.LotSizeFilter), nameof(CreateBuyMarketOrderAsync));
                
                return ActionResult.Error;
            }
            
            if (balance.WalletBalance <= 0)
            {
                _logger.LogWarning("{Position}. Wallet balance is lower or equal to zero. In {Method}",
                    positionString, nameof(CreateBuyMarketOrderAsync));
                    
                return ActionResult.Error;
            }
            
            var initialMargin = symbolInfo.MinNotionalFilter.MinNotional;
            for (var i = 0; i < maxRetries; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("{Position}. CancellationToken is requested. In {Method}",
                        positionString, nameof(CreateBuyMarketOrderAsync));

                    return ActionResult.CancellationTokenRequested;
                }

                var lastPriceRequest = await _restBinanceClient.UsdFuturesApi.ExchangeData.GetPriceAsync(
                    openedPosition.Name,
                    cancellationToken
                );

                if (!lastPriceRequest.Success)
                {
                    _logger.LogWarning(new ThException(lastPriceRequest.Error), "{Position}. During getting last price. In {Method}",
                        positionString, nameof(CreateBuyMarketOrderAsync));
                        
                    continue;
                }
                
                var orderQuantity = _calculatorService.GetOrderQuantity(
                    lastPriceRequest.Data.Price,
                    initialMargin,
                    symbolInfo.LotSizeFilter.MinQuantity
                );
            
                var placeOrderRequest = await _restBinanceClient.UsdFuturesApi.Trading.PlaceOrderAsync(
                    symbol: openedPosition.Name,
                    side: openedPosition.PositionSide == PositionSide.Short ? OrderSide.Sell : OrderSide.Buy,
                    type: FuturesOrderType.Market,
                    quantity: orderQuantity,
                    positionSide: openedPosition.PositionSide,
                    ct: cancellationToken
                );

                if (placeOrderRequest.Success)
                {
                    break;
                }

                _logger.LogWarning(new ThException(placeOrderRequest.Error), "{Position}. Last price: {Price}, Initial margin: {InitialMargin}, Order quantity: {Quantity}. In {Method}",
                    positionString, lastPriceRequest.Data.Price, initialMargin, orderQuantity, nameof(CreateBuyMarketOrderAsync));

                if ((ApiErrorCodes)(placeOrderRequest.Error?.Code ?? 0) == ApiErrorCodes.MaximumExceededAtCurrentLeverage)
                {
                    await _futuresUsdEndpoints.ChangeSymbolLeverageToAvailableAsync(openedPosition.Name, cancellationToken: cancellationToken);
                    
                    continue;
                }
                
                if ((ApiErrorCodes)(placeOrderRequest.Error?.Code ?? 0) != ApiErrorCodes.MinNotionalError)
                {
                    initialMargin += 1;
                    
                    continue;
                }

                if (i != maxRetries - 1)
                {
                    continue;
                }
                
                _logger.LogError("{Position}. {Number} retries exceeded In {Method}",
                    positionString, maxRetries, nameof(CreateBuyMarketOrderAsync));
            }

            _logger.LogInformation("{Position}. Order placed successfully. In {Method}",
                positionString, nameof(CreateBuyMarketOrderAsync));

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
            _logger.LogCritical(exception, "{Position}. In {Method}",
                positionString, nameof(CreateBuyMarketOrderAsync));

            return ActionResult.SystemError;
        }
    }
}