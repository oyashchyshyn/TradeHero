using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Menu;
using TradeHero.Contracts.Services;
using TradeHero.Contracts.Store;

namespace TradeHero.EntryPoint.Menu.Console;

internal class ConsoleMenu : IMenuService
{
    private readonly ILogger<ConsoleMenu> _logger;
    private readonly IEnvironmentService _environmentService;

    public ConsoleMenu(
        ILogger<ConsoleMenu> logger,
        IEnvironmentService environmentService, 
        ITerminalService terminalService
        )
    {
        _logger = logger;
        _environmentService = environmentService;
    }

    public async Task<ActionResult> InitAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return ActionResult.Success;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "In {Method}", nameof(InitAsync));

            return ActionResult.SystemError;
        }
    }

    public async Task<ActionResult> FinishAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return ActionResult.Success;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "In {Method}", nameof(InitAsync));

            return ActionResult.SystemError;
        }
    }

    public async Task<ActionResult> OnDisconnectFromInternetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return ActionResult.Success;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "In {Method}", nameof(InitAsync));

            return ActionResult.SystemError;
        }
    }

    public async Task<ActionResult> OnReconnectToInternetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return ActionResult.Success;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "In {Method}", nameof(InitAsync));

            return ActionResult.SystemError;
        }
    }
}