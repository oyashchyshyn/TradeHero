using Binance.Net.Objects.Models.Futures;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Services;
using TradeHero.Strategies.Strategies.PercentMoveStrategy.Options;

namespace TradeHero.Strategies.Strategies.PercentMoveStrategy.Flow;

internal class PmsFilters
{
    private readonly ILogger<PmsFilters> _logger;
    private readonly ICalculatorService _calculatorService;

    public PmsFilters(
        ILogger<PmsFilters> logger,
        ICalculatorService calculatorService
        )
    {
        _logger = logger;
        _calculatorService = calculatorService;
    }
    
    public bool IsNeedToPlaceOrder(string symbol, decimal lastPrice, decimal lastOrderPrice, BinanceFuturesUsdtSymbol symbolInfo, PmsStrategyOptions strategyOptions)
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
            
            return percentBetweenLastOrderAndLastPrice >= strategyOptions.PricePercentMove;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(IsNeedToPlaceOrder));

            return false;
        }
    }
}