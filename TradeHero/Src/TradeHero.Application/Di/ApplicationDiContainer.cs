using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TradeHero.Application.Data;
using TradeHero.Application.Data.Dtos.Instance;
using TradeHero.Application.Data.Dtos.TradeLogic;
using TradeHero.Application.Data.Validations;
using TradeHero.Application.Dictionary;
using TradeHero.Application.Host;
using TradeHero.Application.Menu;
using TradeHero.Application.Menu.Console;
using TradeHero.Application.Menu.Telegram;
using TradeHero.Core.Contracts.Menu;
using TradeHero.Core.Models.Repositories;

namespace TradeHero.Application.Di;

internal static class ApplicationDiContainer
{
    public static void AddHost(this IServiceCollection serviceCollection, CancellationTokenSource cancellationTokenSource)
    {
        serviceCollection.AddSingleton<ApplicationShutdown>(_ => new ApplicationShutdown(cancellationTokenSource));
        
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