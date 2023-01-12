using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TradeHero.Contracts.Menu;
using TradeHero.Contracts.Repositories.Models;
using TradeHero.Menu.Data;
using TradeHero.Menu.Data.Dtos.Instance;
using TradeHero.Menu.Data.Dtos.TradeLogic;
using TradeHero.Menu.Data.Validations;
using TradeHero.Menu.Dictionary;
using TradeHero.Menu.Menu;
using TradeHero.Menu.Menu.Console;
using TradeHero.Menu.Menu.Telegram;

namespace TradeHero.Menu;

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