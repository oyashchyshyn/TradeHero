using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TradeHero.Contracts.Menu;
using TradeHero.Contracts.Repositories.Models;
using TradeHero.Main.Data;
using TradeHero.Main.Data.Dtos.Instance;
using TradeHero.Main.Data.Dtos.TradeLogic;
using TradeHero.Main.Data.Validations;
using TradeHero.Main.Dictionary;
using TradeHero.Main.Menu;
using TradeHero.Main.Menu.Console;
using TradeHero.Main.Menu.Telegram;

namespace TradeHero.Main;

public static class HostDiContainer
{
    public static void Register(IServiceCollection serviceCollection)
    {
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
    }
}