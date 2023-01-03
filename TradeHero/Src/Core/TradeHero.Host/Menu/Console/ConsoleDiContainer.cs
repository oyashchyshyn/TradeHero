using Microsoft.Extensions.DependencyInjection;

namespace TradeHero.Host.Menu.Console;

internal static class ConsoleDiContainer
{
    public static void Register(IServiceCollection serviceCollection)
    {
        // Telegram Menu
        serviceCollection.AddSingleton<ConsoleMenu>();
    }
}