using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TradeHero.Contracts.EntryPoint;
using TradeHero.EntryPoint.Data;
using TradeHero.EntryPoint.Data.Dtos.Instance;
using TradeHero.EntryPoint.Data.Dtos.Strategy;
using TradeHero.EntryPoint.Data.Validations;
using TradeHero.EntryPoint.Dictionary;
using TradeHero.EntryPoint.Init;
using TradeHero.EntryPoint.Menu.Telegram;
using TradeHero.EntryPoint.Menu.Telegram.Commands;
using TradeHero.EntryPoint.Menu.Telegram.Commands.Bot;
using TradeHero.EntryPoint.Menu.Telegram.Commands.Bot.Commands;
using TradeHero.EntryPoint.Menu.Telegram.Commands.Connection;
using TradeHero.EntryPoint.Menu.Telegram.Commands.Connection.Commands;
using TradeHero.EntryPoint.Menu.Telegram.Commands.Positions;
using TradeHero.EntryPoint.Menu.Telegram.Commands.Positions.Commands;
using TradeHero.EntryPoint.Menu.Telegram.Commands.Strategy;
using TradeHero.EntryPoint.Menu.Telegram.Commands.Strategy.Commands;

namespace TradeHero.EntryPoint;

public static class ThLogicServiceCollectionExtensions
{
    public static void AddThLogic(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddTransient<IStartup, Startup>();
        
        // Dictionary
        serviceCollection.AddSingleton<EnumDictionary>();
        
        // Telegram Menu
        serviceCollection.AddSingleton<TelegramMenu>();
        serviceCollection.AddSingleton<TelegramMenuStore>();
        
        // Main menu
        serviceCollection.AddTransient<MainMenuCommand>();
        
        // Bot
        serviceCollection.AddTransient<BotCommand>();
        serviceCollection.AddTransient<StartStrategyCommand>();
        serviceCollection.AddTransient<StopStrategyCommand>();
        serviceCollection.AddTransient<CheckCodeStatusCommand>();
        serviceCollection.AddTransient<PidorCommand>();
        
        // Positions
        serviceCollection.AddTransient<PositionsCommand>();
        serviceCollection.AddTransient<WatchingPositionsCommand>();

        // Strategy settings
        serviceCollection.AddTransient<StrategyCommand>();
        serviceCollection.AddTransient<AddStrategyCommand>();
        serviceCollection.AddTransient<UpdateStrategyCommand>();
        serviceCollection.AddTransient<SetActiveStrategyCommand>();
        serviceCollection.AddTransient<ShowStrategiesCommand>();
        serviceCollection.AddTransient<ShowStrategiesPropertiesCommand>();
        serviceCollection.AddTransient<DeleteStrategyCommand>();
        
        // Connection settings
        serviceCollection.AddTransient<ConnectionCommand>();
        serviceCollection.AddTransient<TestConnectionCommand>();
        serviceCollection.AddTransient<AddConnectionCommand>();
        serviceCollection.AddTransient<UpdateConnectionCommand>();
        serviceCollection.AddTransient<SetActiveConnectionCommand>();
        serviceCollection.AddTransient<ShowConnectionsCommand>();
        serviceCollection.AddTransient<DeleteConnectionCommand>();
        
        // Data validation
        serviceCollection.AddTransient<IValidator<PercentLimitStrategyDto>, PercentLimitStrategyDtoValidation>();
        serviceCollection.AddTransient<IValidator<PercentMoveStrategyDto>, PercentMoveStrategyDtoValidation>();
        serviceCollection.AddTransient<IValidator<ClusterVolumeInstanceDto>, ClusterVolumeOptionsDtoValidation>();
        serviceCollection.AddSingleton<DtoValidator>();
    }
}