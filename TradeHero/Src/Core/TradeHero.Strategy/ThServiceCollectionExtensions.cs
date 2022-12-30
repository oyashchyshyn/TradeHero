using Microsoft.Extensions.DependencyInjection;
using TradeHero.Contracts.Strategy;
using TradeHero.Strategies.Endpoints.Rest;
using TradeHero.Strategies.Endpoints.Rest.Implementation;
using TradeHero.Strategies.Endpoints.Socket;
using TradeHero.Strategies.Endpoints.Socket.Implementation;
using TradeHero.Strategies.Factory;
using TradeHero.Strategies.Instances;
using TradeHero.Strategies.Strategies.PercentLimitsStrategy;
using TradeHero.Strategies.Strategies.PercentLimitsStrategy.Factory;
using TradeHero.Strategies.Strategies.PercentLimitsStrategy.Flow;
using TradeHero.Strategies.Strategies.PercentLimitsStrategy.Streams;
using TradeHero.Strategies.Strategies.PercentMoveStrategy;
using TradeHero.Strategies.Strategies.PercentMoveStrategy.Factory;
using TradeHero.Strategies.Strategies.PercentMoveStrategy.Flow;
using TradeHero.Strategies.Strategies.PercentMoveStrategy.Streams;

namespace TradeHero.Strategies;

public static class ThStrategyServiceCollectionExtensions
{
    public static void AddThStrategy(this IServiceCollection serviceCollection)
    {
        // Instance
        serviceCollection.AddTransient<IInstanceFactory, InstanceFactory>();
        serviceCollection.AddTransient<ClusterVolumeInstance>();

        // Strategy
        serviceCollection.AddTransient<IStrategyFactory, StrategyFactory>();
        serviceCollection.AddTransient<IFuturesUsdEndpoints, FuturesUsdEndpoints>();
        serviceCollection.AddTransient<ISpotEndpoints, SpotEndpoints>();
        serviceCollection.AddTransient<IFuturesUsdMarketTickerStream, FuturesUsdMarketTickerStream>();
        
        // Percent limit strategy
        serviceCollection.AddSingleton<PlsStore>();
        serviceCollection.AddTransient<PlsFilters>();
        serviceCollection.AddTransient<PlsStrategy>();
        serviceCollection.AddTransient<PlsEndpoints>();
        serviceCollection.AddTransient<PlsPositionWorker>();
        serviceCollection.AddTransient<PlsUserAccountStream>();
        serviceCollection.AddTransient<PlsSymbolTickerStream>();
        serviceCollection.AddTransient<PlsSymbolTickerStreamFactory>();
        
        // Percent move strategy
        serviceCollection.AddSingleton<PmsStore>();
        serviceCollection.AddTransient<PmsFilters>();
        serviceCollection.AddTransient<PmsStrategy>();
        serviceCollection.AddTransient<PmsEndpoints>();
        serviceCollection.AddTransient<PmsPositionWorker>();
        serviceCollection.AddTransient<PmsUserAccountStream>();
        serviceCollection.AddTransient<PmsSymbolTickerStream>();
        serviceCollection.AddTransient<PmsSymbolTickerStreamFactory>();
    }
}