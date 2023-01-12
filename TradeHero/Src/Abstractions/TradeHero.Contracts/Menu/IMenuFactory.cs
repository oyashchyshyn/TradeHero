namespace TradeHero.Contracts.Menu;

public interface IMenuFactory
{
    IEnumerable<IMenuService> GetMenus();
}