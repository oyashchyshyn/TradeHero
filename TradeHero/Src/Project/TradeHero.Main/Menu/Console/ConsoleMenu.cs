using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Menu;
using TradeHero.Core.Enums;

namespace TradeHero.Main.Menu.Console;

internal class ConsoleMenu : IMenuService
{
    private readonly ILogger<ConsoleMenu> _logger;

    public ConsoleMenu(
        ILogger<ConsoleMenu> logger
        )
    {
        _logger = logger;
    }

    public Task<ActionResult> InitAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return Task.FromResult(ActionResult.Success);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "In {Method}", nameof(InitAsync));

            return Task.FromResult(ActionResult.SystemError);
        }
    }

    public Task<ActionResult> FinishAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return Task.FromResult(ActionResult.Success);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "In {Method}", nameof(FinishAsync));

            return Task.FromResult(ActionResult.SystemError);
        }
    }

    public Task<ActionResult> OnDisconnectFromInternetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return Task.FromResult(ActionResult.Success);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "In {Method}", nameof(InitAsync));

            return Task.FromResult(ActionResult.SystemError);
        }
    }

    public Task<ActionResult> OnReconnectToInternetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return Task.FromResult(ActionResult.Success);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "In {Method}", nameof(OnReconnectToInternetAsync));

            return Task.FromResult(ActionResult.SystemError);
        }
    }
}