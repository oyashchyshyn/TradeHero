using Microsoft.Extensions.DependencyInjection;
using TradeHero.Host.Menu.Telegram.Commands;
using TradeHero.Host.Menu.Telegram.Commands.Bot;
using TradeHero.Host.Menu.Telegram.Commands.Bot.Commands;
using TradeHero.Host.Menu.Telegram.Commands.Connection;
using TradeHero.Host.Menu.Telegram.Commands.Connection.Commands;
using TradeHero.Host.Menu.Telegram.Commands.Positions;
using TradeHero.Host.Menu.Telegram.Commands.Positions.Commands;
using TradeHero.Host.Menu.Telegram.Commands.Strategy;
using TradeHero.Host.Menu.Telegram.Commands.Strategy.Commands;
using TradeHero.Host.Menu.Telegram.Store;

namespace TradeHero.Host.Menu.Telegram;

internal static class TelegramDiContainer
{
    public static void Register(IServiceCollection serviceCollection)
    {
        // Telegram Menu
        serviceCollection.AddSingleton<TelegramMenu>();
        serviceCollection.AddSingleton<TelegramMenuStore>();
        
        // Main menu
        serviceCollection.AddTransient<MainMenuCommand>();
        
        // Bot
        serviceCollection.AddTransient<BotCommand>();
        serviceCollection.AddTransient<StartCommand>();
        serviceCollection.AddTransient<StopCommand>();
        serviceCollection.AddTransient<CheckCodeStatusCommand>();
        serviceCollection.AddTransient<AboutCommand>();
        serviceCollection.AddTransient<CheckUpdateCommand>();
        
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
        serviceCollection.AddTransient<AddConnectionCommand>();
        serviceCollection.AddTransient<SetActiveConnectionCommand>();
        serviceCollection.AddTransient<ShowConnectionsCommand>();
        serviceCollection.AddTransient<DeleteConnectionCommand>();
    }
}