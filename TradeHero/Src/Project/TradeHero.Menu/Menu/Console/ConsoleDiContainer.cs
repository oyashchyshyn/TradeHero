using Microsoft.Extensions.DependencyInjection;
using TradeHero.Contracts.Menu;

namespace TradeHero.Menu.Menu.Console;

internal static class ConsoleDiContainer
{
    public static void Register(IServiceCollection serviceCollection)
    {
        // Console Menu
        serviceCollection.AddSingleton<IMenuService, ConsoleMenu>();
    }
}