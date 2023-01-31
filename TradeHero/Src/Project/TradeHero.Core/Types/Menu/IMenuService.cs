using TradeHero.Core.Enums;
using TradeHero.Core.Types.Menu.Models;

namespace TradeHero.Core.Types.Menu;

public interface IMenuService
{
    MenuType MenuType { get; }
    Task<ActionResult> InitAsync(CancellationToken cancellationToken = default);
    Task<ActionResult> FinishAsync(CancellationToken cancellationToken = default);
    Task<ActionResult> SendMessageAsync(string message, SendMessageOptions sendMessageOptions, CancellationToken cancellationToken = default);
}