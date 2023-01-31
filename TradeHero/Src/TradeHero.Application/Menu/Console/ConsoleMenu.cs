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
    private readonly IDateTimeService _dateTimeService;
    
    public MenuType MenuType => MenuType.Console;
    
    public ConsoleMenu(
        ILogger<ConsoleMenu> logger, 
        ITerminalService terminalService, 
        IDateTimeService dateTimeService
        )
    {
        _logger = logger;
        _terminalService = terminalService;
        _dateTimeService = dateTimeService;
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
            if (sendMessageOptions.IsNeedToShowTime)
            {
                _terminalService.Write($"[{_dateTimeService.GetLocalDateTime():HH:mm:ss}]", ConsoleColor.Gray);
                _terminalService.Write(" ");
            }
            
            _terminalService.Write(message);
            
            return Task.FromResult(ActionResult.Success);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "In {Method}", nameof(FinishAsync));

            return Task.FromResult(ActionResult.SystemError);
        }
    }
}