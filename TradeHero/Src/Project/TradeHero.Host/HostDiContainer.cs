using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Repositories.Models;
using TradeHero.Host.Data;
using TradeHero.Host.Data.Dtos.Instance;
using TradeHero.Host.Data.Dtos.TradeLogic;
using TradeHero.Host.Data.Validations;
using TradeHero.Host.Dictionary;
using TradeHero.Host.Host;
using TradeHero.Host.Logger;
using TradeHero.Host.Menu;
using TradeHero.Host.Menu.Console;
using TradeHero.Host.Menu.Telegram;

namespace TradeHero.Host;

internal static class HostDiContainer
{
    public static void Register(IServiceCollection serviceCollection)
    {
        // Logger
        serviceCollection.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog();
        });
        
        // Host
        serviceCollection.AddHostedService<ThHostedService>();
        serviceCollection.AddSingleton<IHostLifetime, ThHostLifeTime>();

        // Menu factory
        serviceCollection.AddSingleton<MenuFactory>();
        
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
    }
}