using TradeHero.Contracts.Base.Enums;

namespace TradeHero.Contracts.Menu;

public interface IMenuService
{
    Task<ActionResult> InitAsync(CancellationToken cancellationToken);
    Task<ActionResult> FinishAsync(CancellationToken cancellationToken);
}