using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Strategy;
using TradeHero.Strategies.Strategies.PercentLimitsStrategy;
using TradeHero.Strategies.Strategies.PercentMoveStrategy;

namespace TradeHero.Strategies.Factory;

internal class StrategyFactory : IStrategyFactory
{
    private readonly ILogger<StrategyFactory> _logger;
    private readonly IServiceProvider _serviceProvider;
    
    public StrategyFactory(
        ILogger<StrategyFactory> logger,
        IServiceProvider serviceProvider
        )
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }
    
    public IStrategy? GetStrategy(StrategyType strategyType)
    {
        try
        {
            IStrategy? strategy = strategyType switch
            {
                StrategyType.PercentLimit => _serviceProvider.GetRequiredService<PlsStrategy>(),
                StrategyType.PercentMove => _serviceProvider.GetRequiredService<PmsStrategy>(),
                StrategyType.NoStrategy => null,
                _ => null
            };

            return strategy;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(GetStrategy));

            return null;
        }
    }
}