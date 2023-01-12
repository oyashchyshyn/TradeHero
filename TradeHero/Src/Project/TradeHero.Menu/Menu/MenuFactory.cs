using Microsoft.Extensions.DependencyInjection;
using TradeHero.Contracts.Menu;

namespace TradeHero.Menu.Menu;

internal class MenuFactory : IMenuFactory
{
    private readonly IServiceProvider _serviceProvider;

    public MenuFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IEnumerable<IMenuService> GetMenus()
    {
        return _serviceProvider.GetServices<IMenuService>();
    }
}