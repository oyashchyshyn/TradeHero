using Microsoft.Extensions.DependencyInjection;
using TradeHero.Contracts.StrategyRunner;
using TradeHero.StrategyRunner.Endpoints.Rest;
using TradeHero.StrategyRunner.Endpoints.Rest.Implementation;
using TradeHero.StrategyRunner.Endpoints.Socket;
using TradeHero.StrategyRunner.Endpoints.Socket.Implementation;
using TradeHero.StrategyRunner.Factory;
using TradeHero.StrategyRunner.Instances;
using TradeHero.StrategyRunner.TradeLogic.PercentLimit;
using TradeHero.StrategyRunner.TradeLogic.PercentLimit.Factory;
using TradeHero.StrategyRunner.TradeLogic.PercentLimit.Flow;
using TradeHero.StrategyRunner.TradeLogic.PercentLimit.Streams;
using TradeHero.StrategyRunner.TradeLogic.PercentMove;
using TradeHero.StrategyRunner.TradeLogic.PercentMove.Factory;
using TradeHero.StrategyRunner.TradeLogic.PercentMove.Flow;
using TradeHero.StrategyRunner.TradeLogic.PercentMove.Streams;

namespace TradeHero.StrategyRunner;

public static class ThStrategyRunnerServiceCollectionExtensions
{
    public static void AddThStrategyRunner(this IServiceCollection serviceCollection)
    {
        // Instance
        serviceCollection.AddTransient<IInstanceFactory, InstanceFactory>();
        serviceCollection.AddTransient<SpotClusterVolumeInstance>();

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