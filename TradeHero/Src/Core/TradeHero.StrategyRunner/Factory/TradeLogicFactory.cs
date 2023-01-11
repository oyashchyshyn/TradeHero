using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.StrategyRunner;
using TradeHero.Core.Enums;
using TradeHero.StrategyRunner.TradeLogic.PercentLimit;
using TradeHero.StrategyRunner.TradeLogic.PercentMove;

namespace TradeHero.StrategyRunner.Factory;

internal class TradeLogicFactory : ITradeLogicFactory
{
    private readonly ILogger<TradeLogicFactory> _logger;
    private readonly IServiceProvider _serviceProvider;
    
    public TradeLogicFactory(
        ILogger<TradeLogicFactory> logger,
        IServiceProvider serviceProvider
        )
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }
    
    public ITradeLogic? GetTradeLogicRunner(TradeLogicType tradeLogicType)
    {
        try
        {
            ITradeLogic? strategy = tradeLogicType switch
            {
                TradeLogicType.PercentLimit => _serviceProvider.GetRequiredService<PercentLimitTradeLogic>(),
                TradeLogicType.PercentMove => _serviceProvider.GetRequiredService<PercentMoveTradeLogic>(),
                TradeLogicType.NoTradeLogic => null,
                _ => null
            };

            return strategy;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(GetTradeLogicRunner));

            return null;
        }
    }
}