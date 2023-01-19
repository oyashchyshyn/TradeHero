namespace TradeHero.Core.Types.Menu;

public interface IMenuFactory
{
    IEnumerable<IMenuService> GetMenus();
}