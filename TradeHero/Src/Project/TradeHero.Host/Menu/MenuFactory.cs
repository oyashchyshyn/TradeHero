using Microsoft.Extensions.DependencyInjection;
using TradeHero.Contracts.Menu;

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
        return _serviceProvider.GetServices<IMenuService>();
    }
}