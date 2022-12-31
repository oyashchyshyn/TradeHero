using Microsoft.Extensions.DependencyInjection;
using TradeHero.Contracts.StrategyRunner;
using TradeHero.Strategies.Endpoints.Rest;
using TradeHero.Strategies.Endpoints.Rest.Implementation;
using TradeHero.Strategies.Endpoints.Socket;
using TradeHero.Strategies.Endpoints.Socket.Implementation;
using TradeHero.Strategies.Factory;
using TradeHero.Strategies.Instances;
using TradeHero.Strategies.TradeLogic.PercentLimit;
using TradeHero.Strategies.TradeLogic.PercentLimit.Factory;
using TradeHero.Strategies.TradeLogic.PercentLimit.Flow;
using TradeHero.Strategies.TradeLogic.PercentLimit.Streams;
using TradeHero.Strategies.TradeLogic.PercentMove;
using TradeHero.Strategies.TradeLogic.PercentMove.Factory;
using TradeHero.Strategies.TradeLogic.PercentMove.Flow;
using TradeHero.Strategies.TradeLogic.PercentMove.Streams;

namespace TradeHero.Strategies;

public static class ThStrategyRunnerServiceCollectionExtensions
{
    public static void AddThStrategyRunner(this IServiceCollection serviceCollection)
    {
        // Instance
        serviceCollection.AddTransient<IInstanceFactory, InstanceFactory>();
        serviceCollection.AddTransient<ClusterVolumeInstance>();

        // Strategy
        serviceCollection.AddTransient<ITradeLogicFactory, TradeLogicFactory>();
        serviceCollection.AddTransient<IFuturesUsdEndpoints, FuturesUsdEndpoints>();
        serviceCollection.AddTransient<ISpotEndpoints, SpotEndpoints>();
        serviceCollection.AddTransient<IFuturesUsdMarketTickerStream, FuturesUsdMarketTickerStream>();
        
        // Percent limit strategy
        serviceCollection.AddSingleton<PercentLimitStore>();
        serviceCollection.AddTransient<PercentLimitFilters>();
        serviceCollection.AddTransient<PercentLimitTradeLogic>();
        serviceCollection.AddTransient<PercentLimitEndpoints>();
        serviceCollection.AddTransient<PercentLimitPositionWorker>();
        serviceCollection.AddTransient<PercentLimitUserAccountStream>();
        serviceCollection.AddTransient<PercentLimitSymbolTickerStream>();
        serviceCollection.AddTransient<PercentMoveSymbolTickerStreamFactory>();
        
        // Percent move strategy
        serviceCollection.AddSingleton<PercentMoveStore>();
        serviceCollection.AddTransient<PercentMoveFilters>();
        serviceCollection.AddTransient<PercentMoveTradeLogic>();
        serviceCollection.AddTransient<PercentMoveEndpoints>();
        serviceCollection.AddTransient<PercentMovePositionWorker>();
        serviceCollection.AddTransient<PercentMoveUserAccountStream>();
        serviceCollection.AddTransient<PercentMoveSymbolTickerStream>();
        serviceCollection.AddTransient<PercentMoveTickerStreamFactory>();
    }
}