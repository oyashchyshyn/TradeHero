using Microsoft.Extensions.DependencyInjection;
using TradeHero.Application.Menu.Telegram.Commands;
using TradeHero.Application.Menu.Telegram.Commands.Bot;
using TradeHero.Application.Menu.Telegram.Commands.Bot.Commands;
using TradeHero.Application.Menu.Telegram.Commands.Connection;
using TradeHero.Application.Menu.Telegram.Commands.Connection.Commands;
using TradeHero.Application.Menu.Telegram.Commands.Positions;
using TradeHero.Application.Menu.Telegram.Commands.Positions.Commands;
using TradeHero.Application.Menu.Telegram.Commands.Strategy;
using TradeHero.Application.Menu.Telegram.Commands.Strategy.Commands;
using TradeHero.Application.Menu.Telegram.Store;
using TradeHero.Core.Contracts.Menu;

namespace TradeHero.Application.Menu.Telegram;

internal static class TelegramDiContainer
{
    public static void Register(IServiceCollection serviceCollection)
    {
        // Telegram Store
        serviceCollection.AddSingleton<TelegramMenuStore>();
        
        // Telegram Menu
        serviceCollection.AddSingleton<IMenuService, TelegramMenu>();

        // Main menu
        serviceCollection.AddTransient<ITelegramMenuCommand, MainTelegramMenuCommand>();
        
        // Bot
        serviceCollection.AddTransient<ITelegramMenuCommand, BotCommand>();
        serviceCollection.AddTransient<ITelegramMenuCommand, StartCommand>();
        serviceCollection.AddTransient<ITelegramMenuCommand, StopCommand>();
        serviceCollection.AddTransient<ITelegramMenuCommand, CheckCodeStatusCommand>();
        serviceCollection.AddTransient<ITelegramMenuCommand, AboutCommand>();
        serviceCollection.AddTransient<ITelegramMenuCommand, CheckUpdateCommand>();
        
        // Positions
        serviceCollection.AddTransient<ITelegramMenuCommand, PositionsCommand>();
        serviceCollection.AddTransient<ITelegramMenuCommand, WatchingPositionsCommand>();

        // Strategy settings
        serviceCollection.AddTransient<ITelegramMenuCommand, StrategyCommand>();
        serviceCollection.AddTransient<ITelegramMenuCommand, AddStrategyCommand>();
        serviceCollection.AddTransient<ITelegramMenuCommand, UpdateStrategyCommand>();
        serviceCollection.AddTransient<ITelegramMenuCommand, SetActiveStrategyCommand>();
        serviceCollection.AddTransient<ITelegramMenuCommand, ShowStrategiesCommand>();
        serviceCollection.AddTransient<ITelegramMenuCommand, ShowStrategiesPropertiesCommand>();
        serviceCollection.AddTransient<ITelegramMenuCommand, DeleteStrategyCommand>();
        
        // Connection settings
        serviceCollection.AddTransient<ITelegramMenuCommand, ConnectionCommand>();
        serviceCollection.AddTransient<ITelegramMenuCommand, AddConnectionCommand>();
        serviceCollection.AddTransient<ITelegramMenuCommand, SetActiveConnectionCommand>();
        serviceCollection.AddTransient<ITelegramMenuCommand, ShowConnectionsCommand>();
        serviceCollection.AddTransient<ITelegramMenuCommand, DeleteConnectionCommand>();
    }
}