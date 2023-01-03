using Microsoft.Extensions.DependencyInjection;
using TradeHero.EntryPoint.Menu.Telegram.Commands;
using TradeHero.EntryPoint.Menu.Telegram.Commands.Bot;
using TradeHero.EntryPoint.Menu.Telegram.Commands.Bot.Commands;
using TradeHero.EntryPoint.Menu.Telegram.Commands.Connection;
using TradeHero.EntryPoint.Menu.Telegram.Commands.Connection.Commands;
using TradeHero.EntryPoint.Menu.Telegram.Commands.Positions;
using TradeHero.EntryPoint.Menu.Telegram.Commands.Positions.Commands;
using TradeHero.EntryPoint.Menu.Telegram.Commands.Strategy;
using TradeHero.EntryPoint.Menu.Telegram.Commands.Strategy.Commands;
using TradeHero.EntryPoint.Menu.Telegram.Store;

namespace TradeHero.EntryPoint.Menu.Telegram;

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
        serviceCollection.AddTransient<AddConnectionCommand>();
        serviceCollection.AddTransient<SetActiveConnectionCommand>();
        serviceCollection.AddTransient<ShowConnectionsCommand>();
        serviceCollection.AddTransient<DeleteConnectionCommand>();
    }
}