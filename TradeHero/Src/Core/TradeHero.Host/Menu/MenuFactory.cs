using Microsoft.Extensions.DependencyInjection;
using TradeHero.Contracts.Menu;
using TradeHero.EntryPoint.Menu.Console;
using TradeHero.EntryPoint.Menu.Telegram;

namespace TradeHero.EntryPoint.Menu;

internal class MenuFactory
{
    private readonly IServiceProvider _serviceProvider;

    public MenuFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IEnumerable<IMenuService> GetMenus()
    {
        return new List<IMenuService>
        {
            _serviceProvider.GetRequiredService<TelegramMenu>(),
            _serviceProvider.GetRequiredService<ConsoleMenu>()
        };
    }
}