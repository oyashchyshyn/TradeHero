using Microsoft.Extensions.DependencyInjection;
using TradeHero.Core.Types.Menu;

namespace TradeHero.Main.Menu.Console;

internal static class ConsoleDiContainer
{
    public static void Register(IServiceCollection serviceCollection)
    {
        // Console Menu
        serviceCollection.AddSingleton<IMenuService, ConsoleMenu>();
    }
}