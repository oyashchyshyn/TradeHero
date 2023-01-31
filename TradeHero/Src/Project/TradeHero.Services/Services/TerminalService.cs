using TradeHero.Core.Contracts.Services;
using TradeHero.Core.Enums;

namespace TradeHero.Services.Services;

internal class TerminalService : ITerminalService
{
    private readonly IEnvironmentService _environmentService;
    private readonly IDateTimeService _dateTimeService;

    public TerminalService(
        IEnvironmentService environmentService, 
        IDateTimeService dateTimeService
        )
    {
        _environmentService = environmentService;
        _dateTimeService = dateTimeService;
    }
    
    public void Write(string message, bool needSendWithTime, ConsoleColor? consoleColor = null)
    {
        if (consoleColor.HasValue)
        {
            Console.ForegroundColor = consoleColor.Value;    
        }

        Console.Write(needSendWithTime ? $"[{_dateTimeService.GetLocalDateTime():HH:mm:ss}] {message}" : message);

        Console.ResetColor();
    }
    
    public void WriteLine(string message, bool needSendWithTime, ConsoleColor? consoleColor = null)
    {
        if (consoleColor.HasValue)
        {
            Console.ForegroundColor = consoleColor.Value;    
        }

        Console.WriteLine(needSendWithTime ? $"[{_dateTimeService.GetLocalDateTime():HH:mm:ss}] {message}" : message);
        
        Console.ResetColor();
    }

    public string? ReadLine()
    {
        return Console.ReadLine();
    }

    public void ClearConsole()
    {
        if (_environmentService.GetEnvironmentType() == EnvironmentType.Development)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Clear console <------------------------------------------------>");
            Console.ResetColor();
            
            return;
        }
        
        Console.Clear();
    }
}