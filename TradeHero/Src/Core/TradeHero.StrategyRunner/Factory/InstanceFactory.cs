using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.StrategyRunner;
using TradeHero.Contracts.StrategyRunner.Models.Instance;
using TradeHero.Strategies.Instances;
using TradeHero.Strategies.Instances.Options;

namespace TradeHero.Strategies.Factory;

internal class InstanceFactory : IInstanceFactory
{
    private readonly ILogger<InstanceFactory> _logger;
    private readonly IServiceProvider _serviceProvider;

    public InstanceFactory(
        ILogger<InstanceFactory> logger, 
        IServiceProvider serviceProvider
        )
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }
    
    public InstanceFactoryResponse? GetInstance(InstanceType instanceType)
    {
        try
        {
            var instance = instanceType switch
            {
                InstanceType.SpotClusterVolume => new InstanceFactoryResponse
                {
                    Instance = _serviceProvider.GetRequiredService<SpotClusterVolumeInstance>(),
                    Type = typeof(SpotClusterVolumeOptions)
                },
                InstanceType.NoInstance => null,
                _ => null
            };

            return instance;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(GetInstance));

            return null;
        }
    }
}