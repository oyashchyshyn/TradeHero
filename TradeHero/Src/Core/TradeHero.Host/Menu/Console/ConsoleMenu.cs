using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Menu;
using TradeHero.Contracts.Services;
using TradeHero.Contracts.Store;

namespace TradeHero.EntryPoint.Menu.Console;

internal class ConsoleMenu : IMenuService
{
    private readonly ILogger<ConsoleMenu> _logger;
    private readonly IStore _store;
    private readonly IUpdateService _updateService;
    private readonly IEnvironmentService _environmentService;

    public ConsoleMenu(
        ILogger<ConsoleMenu> logger, 
        IStore store, 
        IUpdateService updateService, 
        IEnvironmentService environmentService
        )
    {
        _logger = logger;
        _store = store;
        _updateService = updateService;
        _environmentService = environmentService;
    }

    public Task<ActionResult> InitAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(ActionResult.Success);
    }

    public Task<ActionResult> FinishAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(ActionResult.Success);
    }
}