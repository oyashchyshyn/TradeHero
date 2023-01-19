using TradeHero.Core.Enums;

namespace TradeHero.Core.Types.Menu;

public interface IMenuService
{
    Task<ActionResult> InitAsync(CancellationToken cancellationToken = default);
    Task<ActionResult> FinishAsync(CancellationToken cancellationToken = default);
    Task<ActionResult> OnDisconnectFromInternetAsync(CancellationToken cancellationToken = default);
    Task<ActionResult> OnReconnectToInternetAsync(CancellationToken cancellationToken = default);
}