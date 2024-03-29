﻿using Microsoft.Extensions.DependencyInjection;
using TradeHero.Core.Contracts.Trading;
using TradeHero.Trading.Endpoints.Rest;
using TradeHero.Trading.Endpoints.Rest.Implementation;
using TradeHero.Trading.Factory;
using TradeHero.Trading.Instances;
using TradeHero.Trading.Logic.PercentLimit;
using TradeHero.Trading.Logic.PercentLimit.Factory;
using TradeHero.Trading.Logic.PercentLimit.Flow;
using TradeHero.Trading.Logic.PercentLimit.Streams;
using TradeHero.Trading.Logic.PercentMove;
using TradeHero.Trading.Logic.PercentMove.Factory;
using TradeHero.Trading.Logic.PercentMove.Flow;
using TradeHero.Trading.Logic.PercentMove.Streams;

namespace TradeHero.Trading;

public static class TradingDiContainer
{
    public static void AddTrading(this IServiceCollection serviceCollection)
    {
        // Instance
        serviceCollection.AddTransient<IInstanceFactory, InstanceFactory>();
        serviceCollection.AddTransient<SpotClusterVolumeInstance>();

        // Strategy
        serviceCollection.AddTransient<ITradeLogicFactory, TradeLogicFactory>();
        serviceCollection.AddTransient<IFuturesUsdEndpoints, FuturesUsdEndpoints>();
        serviceCollection.AddTransient<ISpotEndpoints, SpotEndpoints>();
        
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