using Microsoft.Extensions.Logging;
using TradeHero.Core.Enums;
using TradeHero.Core.Types.Menu;
using TradeHero.Core.Types.Services;

namespace TradeHero.Main.Menu.Console;

internal class ConsoleMenu : IMenuService
{
    private readonly ILogger<ConsoleMenu> _logger;
    private readonly ITerminalService _terminalService;

    public ConsoleMenu(
        ILogger<ConsoleMenu> logger, 
        ITerminalService terminalService
        )
    {
        _logger = logger;
        _terminalService = terminalService;
    }

    public Task<ActionResult> InitAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _terminalService.WriteLine("Bot started! Please check telegram.");
            
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
            _terminalService.WriteLine("Bot finished!");
            
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
            _terminalService.WriteLine("Internet disconnected.");
            
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
            _terminalService.WriteLine("Internet reconnected.");
            
            return Task.FromResult(ActionResult.Success);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "In {Method}", nameof(OnReconnectToInternetAsync));

            return Task.FromResult(ActionResult.SystemError);
        }
    }
}