namespace TradeHero.Core.Contracts.Menu;

public interface IMenuFactory
{
    IEnumerable<IMenuService> GetMenus();
}