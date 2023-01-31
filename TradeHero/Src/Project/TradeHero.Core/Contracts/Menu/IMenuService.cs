using TradeHero.Core.Enums;
using TradeHero.Core.Models.Menu;

namespace TradeHero.Core.Contracts.Menu;

public interface IMenuService
{
    MenuType MenuType { get; }
    Task<ActionResult> InitAsync(CancellationToken cancellationToken = default);
    Task<ActionResult> FinishAsync(CancellationToken cancellationToken = default);
    Task<ActionResult> SendMessageAsync(string message, SendMessageOptions sendMessageOptions, CancellationToken cancellationToken = default);
}