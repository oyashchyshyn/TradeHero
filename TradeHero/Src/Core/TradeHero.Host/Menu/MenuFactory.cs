using Microsoft.Extensions.DependencyInjection;
using TradeHero.Contracts.Menu;
using TradeHero.Host.Menu.Console;
using TradeHero.Host.Menu.Telegram;

namespace TradeHero.Host.Menu;

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