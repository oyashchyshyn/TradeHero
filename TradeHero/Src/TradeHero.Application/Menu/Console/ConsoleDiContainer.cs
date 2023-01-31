using Microsoft.Extensions.DependencyInjection;
using TradeHero.Core.Contracts.Menu;

namespace TradeHero.Application.Menu.Console;

internal static class ConsoleDiContainer
{
    public static void Register(IServiceCollection serviceCollection)
    {
        // Console Menu
        serviceCollection.AddSingleton<IMenuService, ConsoleMenu>();
    }
}