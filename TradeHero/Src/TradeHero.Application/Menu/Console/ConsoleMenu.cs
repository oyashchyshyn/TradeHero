using Microsoft.Extensions.Logging;
using TradeHero.Core.Contracts.Menu;
using TradeHero.Core.Contracts.Services;
using TradeHero.Core.Enums;
using TradeHero.Core.Models.Menu;

namespace TradeHero.Application.Menu.Console;

internal class ConsoleMenu : IMenuService
{
    private readonly ILogger<ConsoleMenu> _logger;
    private readonly ITerminalService _terminalService;
    
    public MenuType MenuType => MenuType.Console;
    
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

    public Task<ActionResult> SendMessageAsync(string message, SendMessageOptions sendMessageOptions,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _terminalService.WriteLine(message, true);
            
            return Task.FromResult(ActionResult.Success);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "In {Method}", nameof(FinishAsync));

            return Task.FromResult(ActionResult.SystemError);
        }
    }
}