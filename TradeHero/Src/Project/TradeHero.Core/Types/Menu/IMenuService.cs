using TradeHero.Core.Enums;

namespace TradeHero.Core.Types.Menu;

public interface IMenuService
{
    MenuType MenuType { get; }
    Task<ActionResult> InitAsync(CancellationToken cancellationToken = default);
    Task<ActionResult> FinishAsync(CancellationToken cancellationToken = default);
    Task<ActionResult> SendMessageAsync(string message, bool isNeedToShowMenu, CancellationToken cancellationToken = default);
}