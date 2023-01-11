using Microsoft.Extensions.DependencyInjection;
using TradeHero.Contracts.Trading;
using TradeHero.Trading.Endpoints.Rest;
using TradeHero.Trading.Endpoints.Rest.Implementation;
using TradeHero.Trading.Endpoints.Socket;
using TradeHero.Trading.Endpoints.Socket.Implementation;
using TradeHero.Trading.Factory;
using TradeHero.Trading.Instances;
using TradeHero.Trading.TradeLogic.PercentLimit;
using TradeHero.Trading.TradeLogic.PercentLimit.Factory;
using TradeHero.Trading.TradeLogic.PercentLimit.Flow;
using TradeHero.Trading.TradeLogic.PercentLimit.Streams;
using TradeHero.Trading.TradeLogic.PercentMove;
using TradeHero.Trading.TradeLogic.PercentMove.Factory;
using TradeHero.Trading.TradeLogic.PercentMove.Flow;
using TradeHero.Trading.TradeLogic.PercentMove.Streams;

namespace TradeHero.Trading;

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