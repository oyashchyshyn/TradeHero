using Microsoft.Extensions.DependencyInjection;
using TradeHero.Core.Types.Menu;

namespace TradeHero.Application.Menu;

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