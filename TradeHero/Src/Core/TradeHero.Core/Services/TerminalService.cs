using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Services;

namespace TradeHero.Core.Services;

internal class TerminalService : ITerminalService
{
    private readonly IEnvironmentService _environmentService;

    public TerminalService(
        IEnvironmentService environmentService
        )
    {
        _environmentService = environmentService;
    }
    
    public void Write(string message, ConsoleColor? consoleColor = null)
    {
        if (consoleColor.HasValue)
        {
            Console.ForegroundColor = consoleColor.Value;    
        }
        
        Console.Write(message);
        Console.ResetColor();
    }
    
    public void WriteLine(string message, ConsoleColor? consoleColor = null)
    {
        if (consoleColor.HasValue)
        {
            Console.ForegroundColor = consoleColor.Value;    
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