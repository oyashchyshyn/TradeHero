using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradeHero.Application.Bot;
using TradeHero.Application.Data;
using TradeHero.Application.Data.Dtos.Instance;
using TradeHero.Application.Data.Dtos.TradeLogic;
using TradeHero.Application.Data.Validations;
using TradeHero.Application.Dictionary;
using TradeHero.Application.Host;
using TradeHero.Application.Menu;
using TradeHero.Application.Menu.Console;
using TradeHero.Application.Menu.Telegram;
using TradeHero.Core.Contracts.Client;
using TradeHero.Core.Contracts.Menu;
using TradeHero.Core.Contracts.Services;
using TradeHero.Core.Models.Repositories;

namespace TradeHero.Application;

internal static class ApplicationDiContainer
{
    public static void AddHost(this IServiceCollection serviceCollection, CancellationTokenSource cancellationTokenSource)
    {
        // Application shutdown
        serviceCollection.AddSingleton<ApplicationShutdown>(serviceProvider =>
        {
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var socketBinanceClient = serviceProvider.GetRequiredService<IThSocketBinanceClient>();
            var internetConnectionService = serviceProvider.GetRequiredService<IInternetConnectionService>();
            var jobService = serviceProvider.GetRequiredService<IJobService>();
            var botWorker = serviceProvider.GetRequiredService<BotWorker>();

            return new ApplicationShutdown(
                loggerFactory.CreateLogger<ApplicationShutdown>(),
                socketBinanceClient,
                internetConnectionService,
                jobService,
                botWorker,
                cancellationTokenSource
            );
        });
        
        // Bot worker
        serviceCollection.AddSingleton<BotWorker>();
        
        // Menu factory
        serviceCollection.AddSingleton<IMenuFactory, MenuFactory>();
        
        // Menus
        ConsoleDiContainer.Register(serviceCollection);
        TelegramDiContainer.Register(serviceCollection);

        // Dictionary
        serviceCollection.AddSingleton<EnumDictionary>();

        // Data validation
        serviceCollection.AddTransient<IValidator<ConnectionDto>, ConnectionDtoValidation>();
        serviceCollection.AddTransient<IValidator<PercentLimitTradeLogicDto>, PercentLimitStrategyDtoValidation>();
        serviceCollection.AddTransient<IValidator<PercentMoveTradeLogicDto>, PercentMoveStrategyDtoValidation>();
        serviceCollection.AddTransient<IValidator<SpotClusterVolumeOptionsDto>, SpotClusterVolumeOptionsDtoValidation>();
        serviceCollection.AddSingleton<DtoValidator>();
        
        // Host
        serviceCollection.AddSingleton<IHostLifetime, AppHostLifeTime>();
        serviceCollection.AddHostedService<AppHostedService>();
    }
}