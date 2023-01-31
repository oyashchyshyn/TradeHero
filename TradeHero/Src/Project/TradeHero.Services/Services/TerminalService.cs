using TradeHero.Core.Contracts.Services;
using TradeHero.Core.Enums;

namespace TradeHero.Services.Services;

internal class TerminalService : ITerminalService
{
    private readonly IEnvironmentService _environmentService;

    public TerminalService(IEnvironmentService environmentService)
    {
        _environmentService = environmentService;
    }
    
    public void Write(string message, ConsoleColor? consoleColor = null, ConsoleColor? backgroundColor = null)
    {
        if (consoleColor.HasValue)
        {
            Console.ForegroundColor = consoleColor.Value;    
        }

        if (backgroundColor.HasValue)
        {
            Console.BackgroundColor = backgroundColor.Value;
        }
        
        Console.Write(message);

        Console.ResetColor();
    }
    
    public void WriteLine(string message, ConsoleColor? foregroundColor = null, ConsoleColor? backgroundColor = null)
    {
        if (foregroundColor.HasValue)
        {
            Console.ForegroundColor = foregroundColor.Value;
        }

        if (backgroundColor.HasValue)
        {
            Console.BackgroundColor = backgroundColor.Value;
        }

        Console.WriteLine(message);
        
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