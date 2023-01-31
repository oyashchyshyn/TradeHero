using Binance.Net.Objects.Models.Futures;
using Microsoft.Extensions.Logging;
using TradeHero.Core.Contracts.Services;
using TradeHero.Trading.Logic.PercentMove.Options;

namespace TradeHero.Trading.Logic.PercentMove.Flow;

internal class PercentMoveFilters
{
    private readonly ILogger<PercentMoveFilters> _logger;
    private readonly ICalculatorService _calculatorService;

    public PercentMoveFilters(
        ILogger<PercentMoveFilters> logger,
        ICalculatorService calculatorService
        )
    {
        _logger = logger;
        _calculatorService = calculatorService;
    }
    
    public bool IsNeedToPlaceOrder(string symbol, decimal lastPrice, decimal lastOrderPrice, BinanceFuturesUsdtSymbol symbolInfo, PercentMoveTradeLogicOptions tradeLogicOptions)
    {
        try
        {
            if (symbolInfo.PriceFilter == null)
            {
                _logger.LogError("{Position}. {Filter} is null. In {Method}",
                    symbol, nameof(symbolInfo.PriceFilter), nameof(IsNeedToPlaceOrder));
                
                return false;
            }

            var percentBetweenLastOrderAndLastPrice = Math.Abs(
                _calculatorService.GetPercentBetweenTwoPrices(
                    lastPrice, 
                    lastOrderPrice
                )
            );
            
            return percentBetweenLastOrderAndLastPrice >= tradeLogicOptions.PricePercentMove;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(IsNeedToPlaceOrder));

            return false;
        }
    }
}