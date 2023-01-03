using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TradeHero.Contracts.Repositories.Models;
using TradeHero.EntryPoint.Data;
using TradeHero.EntryPoint.Data.Dtos.Instance;
using TradeHero.EntryPoint.Data.Dtos.TradeLogic;
using TradeHero.EntryPoint.Data.Validations;
using TradeHero.EntryPoint.Dictionary;
using TradeHero.EntryPoint.Host;
using TradeHero.EntryPoint.Menu;
using TradeHero.EntryPoint.Menu.Console;
using TradeHero.EntryPoint.Menu.Telegram;

namespace TradeHero.EntryPoint;

public static class ThLogicServiceCollectionExtensions
{
    public static void AddThHost(this IServiceCollection serviceCollection)
    {
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
        
        // Host
        serviceCollection.AddSingleton<IHostLifetime, ThHostLifeTime>();
    }
}